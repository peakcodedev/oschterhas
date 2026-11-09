import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, exhaustMap, map, Observable, of, switchMap, takeUntil } from 'rxjs';
import { PublicApiService } from '../../../core/api/public-api.service';
import { GEOLOCATION } from '../../../core/geolocation';
import { geolocationActions, publicInstanceApiActions, searchPageActions } from './search.actions';
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
