import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { Me } from '../api/api-types';

export const authActions = createActionGroup({
  source: 'Auth',
  events: {
    checkRequested: emptyProps(),
    signedIn: props<{ user: Me }>(),
    anonymous: emptyProps(),
    checkFailed: emptyProps(),
    loginClicked: props<{ returnUrl: string }>(),
    logoutClicked: emptyProps(),
    /** Any admin request answered 401: the session expired. */
    sessionExpired: emptyProps(),
  },
});
