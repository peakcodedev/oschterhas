import { createFeature, createReducer, on } from '@ngrx/store';
import { Me } from '../api/api-types';
import { authActions } from './auth.actions';

export type AuthStatus = 'unknown' | 'checking' | 'signedIn' | 'anonymous' | 'error';

export interface AuthState {
  status: AuthStatus;
  user: Me | null;
}

export const initialAuthState: AuthState = { status: 'unknown', user: null };

export const authFeature = createFeature({
  name: 'auth',
  reducer: createReducer(
    initialAuthState,
    on(authActions.checkRequested, (state): AuthState => ({ ...state, status: 'checking' })),
    on(authActions.signedIn, (_, { user }): AuthState => ({ status: 'signedIn', user })),
    on(authActions.anonymous, authActions.sessionExpired, (): AuthState => ({
      status: 'anonymous',
      user: null,
    })),
    on(authActions.checkFailed, (state): AuthState => ({ ...state, status: 'error' })),
  ),
});
