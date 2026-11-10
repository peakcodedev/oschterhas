import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Action } from '@ngrx/store';
import { provideMockActions } from '@ngrx/effects/testing';
import { firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { HareFound, PublicInstance } from '../../../core/api/api-types';
import { EVENT_SOURCE_FACTORY } from '../../../core/api/event-source';
import { PublicApiService } from '../../../core/api/public-api.service';
import { GEOLOCATION } from '../../../core/geolocation';
import {
  geolocationActions,
  instanceEventsActions,
  publicInstanceApiActions,
  searchPageActions,
} from './search.actions';
import {
  liveUpdates,
  loadInstance,
  markFound,
  reloadAfterConflict,
  reloadInstance,
  watchPosition,
} from './search.effects';

const instance: PublicInstance = {
  name: 'Tann',
  description: null,
  eventDate: null,
  status: 'Active',
  totalHares: 0,
  foundHares: 0,
  hares: [],
};

/** Minimal EventSource stand-in that lets tests emit server events. */
class FakeEventSource {
  readyState = 1;
  onerror: (() => void) | null = null;
  closed = false;
  private readonly listeners = new Map<string, ((event: MessageEvent) => void)[]>();

  addEventListener(type: string, listener: (event: MessageEvent) => void): void {
    this.listeners.set(type, [...(this.listeners.get(type) ?? []), listener]);
  }

  emit(type: string, data: unknown = {}): void {
    for (const listener of this.listeners.get(type) ?? []) {
      listener(
        new MessageEvent(type, { data: typeof data === 'string' ? data : JSON.stringify(data) }),
      );
    }
  }

  fail(closed: boolean): void {
    this.readyState = closed ? 2 : 0;
    this.onerror?.();
  }

  close(): void {
    this.closed = true;
  }
}

function setup(options: {
  api?: Partial<PublicApiService>;
  geolocation?: Geolocation | null;
  sources?: FakeEventSource[];
}) {
  const actions$ = new Subject<Action>();
  TestBed.configureTestingModule({
    providers: [
      provideMockActions(() => actions$ as Observable<Action>),
      { provide: PublicApiService, useValue: options.api ?? {} },
      { provide: GEOLOCATION, useValue: options.geolocation ?? null },
      {
        provide: EVENT_SOURCE_FACTORY,
        useValue: (url: string) => {
          expect(url).toBe('/api/public/events?ngsw-bypass=true');
          const source = new FakeEventSource();
          options.sources?.push(source);
          return source as unknown as EventSource;
        },
      },
    ],
  });
  return actions$;
}

describe('loadInstance', () => {
  it('emits loadSucceeded with the instance', async () => {
    const actions$ = setup({ api: { getInstance: () => of(instance) } });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => loadInstance()));

    actions$.next(searchPageActions.opened());

    expect(await result).toEqual(publicInstanceApiActions.loadSucceeded({ instance }));
  });

  it.each([
    [404, 'notFound'],
    [500, 'error'],
    [0, 'error'],
  ] as const)('maps HTTP %s to %s', async (status, reason) => {
    const actions$ = setup({
      api: { getInstance: () => throwError(() => new HttpErrorResponse({ status })) },
    });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => loadInstance()));

    actions$.next(searchPageActions.retryClicked());

    expect(await result).toEqual(publicInstanceApiActions.loadFailed({ reason }));
  });
});

describe('watchPosition', () => {
  it('reports unsupported when the browser has no geolocation', async () => {
    const actions$ = setup({ geolocation: null });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => watchPosition()));

    actions$.next(searchPageActions.locateMeClicked());

    expect(await result).toEqual(geolocationActions.failed({ reason: 'unsupported' }));
  });

  it('emits positions and stops watching when the page closes', () => {
    let onSuccess: PositionCallback = () => undefined;
    const clearWatch = vi.fn();
    const geolocation = {
      watchPosition: (success: PositionCallback) => {
        onSuccess = success;
        return 42;
      },
      clearWatch,
    } as unknown as Geolocation;
    const actions$ = setup({ geolocation });
    const emitted: Action[] = [];
    const subscription = TestBed.runInInjectionContext(() => watchPosition()).subscribe((a) =>
      emitted.push(a),
    );

    actions$.next(searchPageActions.locateMeClicked());
    onSuccess({ coords: { latitude: 47.1, longitude: 8.1, accuracy: 10 } } as GeolocationPosition);
    onSuccess({ coords: { latitude: 47.2, longitude: 8.2, accuracy: 5 } } as GeolocationPosition);
    expect(clearWatch).not.toHaveBeenCalled();

    actions$.next(searchPageActions.closed());

    expect(emitted).toEqual([
      geolocationActions.positionChanged({ position: { lat: 47.1, lon: 8.1, accuracy: 10 } }),
      geolocationActions.positionChanged({ position: { lat: 47.2, lon: 8.2, accuracy: 5 } }),
    ]);
    expect(clearWatch).toHaveBeenCalledWith(42);
    subscription.unsubscribe();
  });

  it('reports denied permission', async () => {
    const geolocation = {
      watchPosition: (_: PositionCallback, error: PositionErrorCallback) => {
        error({ code: 1, PERMISSION_DENIED: 1 } as GeolocationPositionError);
        return 1;
      },
      clearWatch: vi.fn(),
    } as unknown as Geolocation;
    const actions$ = setup({ geolocation });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => watchPosition()));

    actions$.next(searchPageActions.locateMeClicked());

    expect(await result).toEqual(geolocationActions.failed({ reason: 'denied' }));
  });
});

describe('markFound', () => {
  const found: HareFound = { hareId: 'h1', foundAt: '2027-03-28T10:00:00Z', foundBy: 'Urs' };

  it('emits markFoundSucceeded', async () => {
    const markFoundApi = vi.fn(() => of(found));
    const actions$ = setup({ api: { markFound: markFoundApi } });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => markFound()));

    actions$.next(searchPageActions.markFoundSubmitted({ hareId: 'h1', nickname: 'Urs' }));

    expect(await result).toEqual(publicInstanceApiActions.markFoundSucceeded({ found }));
    expect(markFoundApi).toHaveBeenCalledWith('h1', 'Urs');
  });

  it('emits markFoundFailed with the mapped reason', async () => {
    const actions$ = setup({
      api: {
        markFound: () =>
          throwError(
            () => new HttpErrorResponse({ status: 409, error: { code: 'hare.already_found' } }),
          ),
      },
    });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => markFound()));

    actions$.next(searchPageActions.markFoundSubmitted({ hareId: 'h1', nickname: null }));

    expect(await result).toEqual(
      publicInstanceApiActions.markFoundFailed({ hareId: 'h1', reason: 'alreadyFound' }),
    );
  });
});

describe('reloadAfterConflict', () => {
  it.each(['alreadyFound', 'notActive'] as const)('reloads after %s', async (reason) => {
    const actions$ = setup({});
    const result = firstValueFrom(TestBed.runInInjectionContext(() => reloadAfterConflict()));

    actions$.next(publicInstanceApiActions.markFoundFailed({ hareId: 'h1', reason }));

    expect(await result).toEqual(instanceEventsActions.reloadRequested());
  });
});

describe('reloadInstance', () => {
  it('keeps the page on a failed background reload', async () => {
    const actions$ = setup({ api: { getInstance: () => throwError(() => new Error('offline')) } });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => reloadInstance()));

    actions$.next(instanceEventsActions.reloadRequested());

    expect(await result).toEqual(publicInstanceApiActions.reloadFailed());
  });
});

describe('liveUpdates', () => {
  function start() {
    const sources: FakeEventSource[] = [];
    const actions$ = setup({ sources });
    const emitted: Action[] = [];
    const subscription = TestBed.runInInjectionContext(() => liveUpdates()).subscribe((a) =>
      emitted.push(a),
    );
    actions$.next(searchPageActions.opened());
    return { actions$, sources, emitted, subscription };
  }

  it('turns server events into actions', () => {
    const { sources, emitted, subscription } = start();
    const found: HareFound = { hareId: 'h1', foundAt: '2027-03-28T10:00:00Z', foundBy: 'Lina' };

    sources[0].emit('ready');
    sources[0].emit('hare.found', found);
    sources[0].emit('hare.reset', { hareId: 'h2' });
    sources[0].emit('hares.changed');
    sources[0].emit('instance.status', { status: 'Ended' });
    sources[0].emit('ping');
    sources[0].emit('hare.found', 'not json');

    expect(emitted).toEqual([
      instanceEventsActions.connected(),
      instanceEventsActions.hareFound({ found }),
      instanceEventsActions.hareReset({ hareId: 'h2' }),
      instanceEventsActions.reloadRequested(),
      instanceEventsActions.reloadRequested(),
    ]);
    subscription.unsubscribe();
  });

  it('reloads after the browser reconnected by itself', () => {
    const { sources, emitted, subscription } = start();

    sources[0].emit('ready');
    sources[0].fail(false);
    sources[0].emit('ready');

    expect(emitted).toEqual([
      instanceEventsActions.connected(),
      instanceEventsActions.disconnected(),
      instanceEventsActions.connected(),
      instanceEventsActions.reloadRequested(),
    ]);
    expect(sources).toHaveLength(1);
    subscription.unsubscribe();
  });

  it('opens a new connection with backoff when the stream was closed', () => {
    vi.useFakeTimers();
    try {
      const { sources, emitted, subscription } = start();
      sources[0].emit('ready');

      sources[0].fail(true);
      expect(sources[0].closed).toBe(true);
      expect(sources).toHaveLength(1);

      vi.advanceTimersByTime(1000);
      expect(sources).toHaveLength(2);

      sources[1].emit('ready');
      expect(emitted.slice(-2)).toEqual([
        instanceEventsActions.connected(),
        instanceEventsActions.reloadRequested(),
      ]);
      subscription.unsubscribe();
    } finally {
      vi.useRealTimers();
    }
  });

  it('waits longer after each failed attempt and resets after a successful connection', () => {
    vi.useFakeTimers();
    try {
      const { sources, subscription } = start();

      sources[0].fail(true);
      vi.advanceTimersByTime(1000);
      expect(sources).toHaveLength(2);

      sources[1].fail(true);
      vi.advanceTimersByTime(1999);
      expect(sources).toHaveLength(2);
      vi.advanceTimersByTime(1);
      expect(sources).toHaveLength(3);

      sources[2].fail(true);
      vi.advanceTimersByTime(4000);
      expect(sources).toHaveLength(4);

      sources[3].emit('ready');
      sources[3].fail(true);
      vi.advanceTimersByTime(1000);
      expect(sources).toHaveLength(5);
      subscription.unsubscribe();
    } finally {
      vi.useRealTimers();
    }
  });

  it('reconnects when the server stays silent (half-open connection)', () => {
    vi.useFakeTimers();
    try {
      const { sources, emitted, subscription } = start();
      sources[0].emit('ready');

      // Pings keep the connection alive.
      vi.advanceTimersByTime(50_000);
      sources[0].emit('ping');
      vi.advanceTimersByTime(50_000);
      expect(sources).toHaveLength(1);

      // Then the server goes silent.
      vi.advanceTimersByTime(20_000);
      expect(sources[0].closed).toBe(true);
      expect(emitted.at(-1)).toEqual(instanceEventsActions.disconnected());

      vi.advanceTimersByTime(1000);
      expect(sources).toHaveLength(2);
      subscription.unsubscribe();
    } finally {
      vi.useRealTimers();
    }
  });

  it('closes the connection when the page closes', () => {
    const { actions$, sources, subscription } = start();

    actions$.next(searchPageActions.closed());

    expect(sources[0].closed).toBe(true);
    subscription.unsubscribe();
  });
});
