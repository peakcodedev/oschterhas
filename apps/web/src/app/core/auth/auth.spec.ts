import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { AdminApiService } from '../api/admin-api.service';
import { Me } from '../api/api-types';
import { authActions } from './auth.actions';
import { checkSession, login, logout } from './auth.effects';
import { authFeature, initialAuthState } from './auth.reducer';
import { BrowserNavigation } from './browser-navigation';

const pia: Me = { id: 'u1', email: 'pia@test', displayName: 'Pia', isPlatformAdmin: true };

describe('auth reducer', () => {
  const reduce = (...actions: Action[]) => actions.reduce(authFeature.reducer, initialAuthState);

  it('tracks the session', () => {
    expect(reduce(authActions.checkRequested()).status).toBe('checking');
    expect(reduce(authActions.signedIn({ user: pia }))).toEqual({ status: 'signedIn', user: pia });
    expect(reduce(authActions.signedIn({ user: pia }), authActions.sessionExpired())).toEqual({
      status: 'anonymous',
      user: null,
    });
    expect(reduce(authActions.checkFailed()).status).toBe('error');
  });
});

describe('auth effects', () => {
  function setup(api: Partial<AdminApiService>, navigation: Partial<BrowserNavigation> = {}) {
    const actions$ = new Subject<Action>();
    TestBed.configureTestingModule({
      providers: [
        provideMockActions(() => actions$ as Observable<Action>),
        { provide: AdminApiService, useValue: api },
        { provide: BrowserNavigation, useValue: navigation },
      ],
    });
    return actions$;
  }

  it.each([
    [of(pia), authActions.signedIn({ user: pia })],
    [throwError(() => new HttpErrorResponse({ status: 401 })), authActions.anonymous()],
    [throwError(() => new HttpErrorResponse({ status: 0 })), authActions.checkFailed()],
  ])('checks the session', async (response, expected) => {
    const actions$ = setup({ me: () => response as Observable<Me> });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => checkSession()));

    actions$.next(authActions.checkRequested());

    expect(await result).toEqual(expected);
  });

  it('starts the login with an encoded return path', () => {
    const goTo = vi.fn();
    const actions$ = setup({}, { goTo });
    const subscription = TestBed.runInInjectionContext(() => login()).subscribe();

    actions$.next(authActions.loginClicked({ returnUrl: '/instanzen/42' }));

    expect(goTo).toHaveBeenCalledWith('/api/auth/login?returnUrl=%2Finstanzen%2F42');
    subscription.unsubscribe();
  });

  it('logs out with a form post carrying the CSRF token', () => {
    const postForm = vi.fn();
    const actions$ = setup(
      {},
      { postForm, cookie: (name: string) => (name === 'XSRF-TOKEN' ? 'token-1' : null) },
    );
    const subscription = TestBed.runInInjectionContext(() => logout()).subscribe();

    actions$.next(authActions.logoutClicked());

    expect(postForm).toHaveBeenCalledWith('/api/auth/logout', {
      __RequestVerificationToken: 'token-1',
    });
    subscription.unsubscribe();
  });
});
