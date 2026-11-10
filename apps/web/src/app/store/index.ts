import { routerReducer, RouterReducerState } from '@ngrx/router-store';
import { ActionReducerMap } from '@ngrx/store';
import { uiFeature, UiState } from './ui/ui.reducer';

/** Root state (router, UI language). Feature states (search, admin, auth) are added with provideState() in their routes. */
export interface AppState {
  router: RouterReducerState;
  ui: UiState;
}

export const reducers: ActionReducerMap<AppState> = {
  router: routerReducer,
  ui: uiFeature.reducer,
};
