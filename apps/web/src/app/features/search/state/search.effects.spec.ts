import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Action } from '@ngrx/store';
import { provideMockActions } from '@ngrx/effects/testing';
import { firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { PublicInstance } from '../../../core/api/api-types';
import { PublicApiService } from '../../../core/api/public-api.service';
import { GEOLOCATION } from '../../../core/geolocation';
import { geolocationActions, publicInstanceApiActions, searchPageActions } from './search.actions';
import { loadInstance, watchPosition } from './search.effects';

const instance: PublicInstance = {
  name: 'Tann',
  description: null,
  eventDate: null,
  status: 'Active',
  totalHares: 0,
  foundHares: 0,
  hares: [],
};

function setup(options: { api?: Partial<PublicApiService>; geolocation?: Geolocation | null }) {
  const actions$ = new Subject<Action>();
  TestBed.configureTestingModule({
    providers: [
      provideMockActions(() => actions$ as Observable<Action>),
      { provide: PublicApiService, useValue: options.api ?? {} },
      { provide: GEOLOCATION, useValue: options.geolocation ?? null },
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
