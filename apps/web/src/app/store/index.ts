import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import { ActionReducerMap } from '@ngrx/store';

/** Root state. Feature states (search, admin, auth) are added with provideState() in their routes. */
export interface AppState {
  router: RouterReducerState;
}

export const reducers: ActionReducerMap<AppState> = {
  router: routerReducer,
};
