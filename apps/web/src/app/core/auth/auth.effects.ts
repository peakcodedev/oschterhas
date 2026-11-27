// Only effects may be exported from this file: app.routes.ts passes the whole module to provideEffects().
import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, of, switchMap, tap } from 'rxjs';
import { AdminApiService } from '../api/admin-api.service';
import { authActions } from './auth.actions';
import { BrowserNavigation } from './browser-navigation';

export const checkSession = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(authActions.checkRequested),
      switchMap(() =>
        api.me().pipe(
          map((user) => authActions.signedIn({ user })),
          catchError((error: unknown) =>
            of(
              error instanceof HttpErrorResponse && error.status === 401
                ? authActions.anonymous()
                : authActions.checkFailed(),
            ),
          ),
        ),
      ),
    ),
  { functional: true },
);

export const login = createEffect(
  (actions$ = inject(Actions), navigation = inject(BrowserNavigation)) =>
    actions$.pipe(
      ofType(authActions.loginClicked),
      tap(({ returnUrl }) =>
        navigation.goTo(`/api/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`),
      ),
    ),
  { functional: true, dispatch: false },
);

export const logout = createEffect(
  (actions$ = inject(Actions), navigation = inject(BrowserNavigation)) =>
    actions$.pipe(
      ofType(authActions.logoutClicked),
      tap(() =>
        navigation.postForm('/api/auth/logout', {
          __RequestVerificationToken: navigation.cookie('XSRF-TOKEN') ?? '',
        }),
      ),
    ),
  { functional: true, dispatch: false },
);
