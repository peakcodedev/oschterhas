// Only effects may be exported from this file: app.routes.ts passes the whole module to provideEffects().
import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import {
  catchError,
  exhaustMap,
  filter,
  map,
  Observable,
  of,
  retry,
  switchMap,
  takeUntil,
  timer,
} from 'rxjs';
import { HareFound } from '../../../core/api/api-types';
import {
  EVENT_SOURCE_FACTORY,
  EventSourceFactory,
  PUBLIC_EVENTS_URL,
} from '../../../core/api/event-source';
import { PublicApiService } from '../../../core/api/public-api.service';
import { GEOLOCATION } from '../../../core/geolocation';
import {
  geolocationActions,
  instanceEventsActions,
  publicInstanceApiActions,
  searchPageActions,
} from './search.actions';
import { toMarkFoundFailure } from './mark-found-failure';
import { GeoPosition, PositionFailure } from './search.models';

export const loadInstance = createEffect(
  (actions$ = inject(Actions), api = inject(PublicApiService)) =>
    actions$.pipe(
      ofType(searchPageActions.opened, searchPageActions.retryClicked),
      switchMap(() =>
        api.getInstance().pipe(
          map((instance) => publicInstanceApiActions.loadSucceeded({ instance })),
          catchError((error: unknown) =>
            of(
              publicInstanceApiActions.loadFailed({
                reason:
                  error instanceof HttpErrorResponse && error.status === 404 ? 'notFound' : 'error',
              }),
            ),
          ),
        ),
      ),
    ),
  { functional: true },
);

/** Background reload: keeps the map on screen, unlike the initial load. */
export const reloadInstance = createEffect(
  (actions$ = inject(Actions), api = inject(PublicApiService)) =>
    actions$.pipe(
      ofType(instanceEventsActions.reloadRequested),
      switchMap(() =>
        api.getInstance().pipe(
          map((instance) => publicInstanceApiActions.loadSucceeded({ instance })),
          catchError(() => of(publicInstanceApiActions.reloadFailed())),
        ),
      ),
    ),
  { functional: true },
);

export const markFound = createEffect(
  (actions$ = inject(Actions), api = inject(PublicApiService)) =>
    actions$.pipe(
      ofType(searchPageActions.markFoundSubmitted),
      exhaustMap(({ hareId, nickname }) =>
        api.markFound(hareId, nickname).pipe(
          map((found) => publicInstanceApiActions.markFoundSucceeded({ found })),
          catchError((error: unknown) =>
            of(
              publicInstanceApiActions.markFoundFailed({
                hareId,
                reason: toMarkFoundFailure(error),
              }),
            ),
          ),
        ),
      ),
    ),
  { functional: true },
);

/** After a conflict our view is outdated (e.g. a missed event): reload. */
export const reloadAfterConflict = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(publicInstanceApiActions.markFoundFailed),
      filter(({ reason }) => reason === 'alreadyFound' || reason === 'notActive'),
      map(() => instanceEventsActions.reloadRequested()),
    ),
  { functional: true },
);

/** Live updates while the page is open. Reconnects with backoff; every reconnect triggers a full reload. */
export const liveUpdates = createEffect(
  (actions$ = inject(Actions), createEventSource = inject(EVENT_SOURCE_FACTORY)) =>
    actions$.pipe(
      ofType(searchPageActions.opened),
      switchMap(() =>
        connect(createEventSource).pipe(takeUntil(actions$.pipe(ofType(searchPageActions.closed)))),
      ),
    ),
  { functional: true },
);

/** Watches the device position after "locate me" until the page is closed. */
export const watchPosition = createEffect(
  (actions$ = inject(Actions), geolocation = inject(GEOLOCATION)) =>
    actions$.pipe(
      ofType(searchPageActions.locateMeClicked),
      exhaustMap(() =>
        watch(geolocation).pipe(
          map((position) => geolocationActions.positionChanged({ position })),
          catchError((reason: PositionFailure) => of(geolocationActions.failed({ reason }))),
          takeUntil(actions$.pipe(ofType(searchPageActions.closed))),
        ),
      ),
    ),
  { functional: true },
);

const maxReconnectDelayMs = 30_000;
/** Same as EventSource.CLOSED; a literal so tests do not need a global EventSource. */
const EVENT_SOURCE_CLOSED = 2;

/** The server sends a "ping" every 25 s; silence for longer means the connection is dead (e.g. network switch). */
const liveSilenceTimeoutMs = 60_000;
const silenceCheckIntervalMs = 10_000;

function connect(createEventSource: EventSourceFactory): Observable<Action> {
  // Shared across reconnects: every "ready" after the first means we may have missed events.
  let connectedBefore = false;
  // Failed attempts since the last "ready"; drives the backoff.
  let failures = 0;

  return new Observable<Action>((subscriber) => {
    const source = createEventSource(PUBLIC_EVENTS_URL);
    let lastMessageAt = Date.now();
    const listen = (type: string, handle: (event: Event) => void = () => undefined) =>
      source.addEventListener(type, (event) => {
        lastMessageAt = Date.now();
        handle(event);
      });

    listen('ready', () => {
      failures = 0;
      subscriber.next(instanceEventsActions.connected());
      if (connectedBefore) {
        subscriber.next(instanceEventsActions.reloadRequested());
      }
      connectedBefore = true;
    });
    listen('ping');
    listen('hare.found', (event) => {
      const found = parse<HareFound>(event);
      if (found) {
        subscriber.next(instanceEventsActions.hareFound({ found }));
      }
    });
    listen('hare.reset', (event) => {
      const data = parse<{ hareId: string }>(event);
      if (data) {
        subscriber.next(instanceEventsActions.hareReset({ hareId: data.hareId }));
      }
    });
    for (const type of ['hares.changed', 'instance.status']) {
      listen(type, () => subscriber.next(instanceEventsActions.reloadRequested()));
    }
    source.onerror = () => {
      subscriber.next(instanceEventsActions.disconnected());
      // The browser retries by itself unless the connection was closed for good (e.g. HTTP error).
      if (source.readyState === EVENT_SOURCE_CLOSED) {
        subscriber.error(new Error('Event stream closed'));
      }
    };

    const watchdog = setInterval(() => {
      if (Date.now() - lastMessageAt > liveSilenceTimeoutMs) {
        subscriber.next(instanceEventsActions.disconnected());
        subscriber.error(new Error('Event stream silent'));
      }
    }, silenceCheckIntervalMs);

    return () => {
      clearInterval(watchdog);
      source.close();
    };
  }).pipe(
    retry({
      delay: () => timer(Math.min(maxReconnectDelayMs, 1000 * 2 ** failures++)),
    }),
  );
}

function parse<T>(event: Event): T | null {
  try {
    return JSON.parse((event as MessageEvent<string>).data) as T;
  } catch {
    return null;
  }
}

function watch(geolocation: Geolocation | null): Observable<GeoPosition> {
  return new Observable<GeoPosition>((subscriber) => {
    if (!geolocation) {
      subscriber.error('unsupported' satisfies PositionFailure);
      return;
    }
    const id = geolocation.watchPosition(
      (p) =>
        subscriber.next({
          lat: p.coords.latitude,
          lon: p.coords.longitude,
          accuracy: p.coords.accuracy,
        }),
      (e) =>
        subscriber.error(
          (e.code === e.PERMISSION_DENIED ? 'denied' : 'unavailable') satisfies PositionFailure,
        ),
      { enableHighAccuracy: true, maximumAge: 10_000 },
    );
    return () => geolocation.clearWatch(id);
  });
}
