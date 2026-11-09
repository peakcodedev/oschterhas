import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { PublicHare } from '../../../core/api/api-types';
import { geolocationActions, publicInstanceApiActions, searchPageActions } from './search.actions';
import { GeoPosition, InstanceInfo, LoadStatus, PositionFailure, Progress } from './search.models';

export interface SearchState extends EntityState<PublicHare> {
  loadStatus: LoadStatus;
  instance: InstanceInfo | null;
  /** Counts from the server; used while hares are hidden (draft). */
  serverProgress: Progress;
  locating: boolean;
  position: GeoPosition | null;
  positionError: PositionFailure | null;
}

export const hareAdapter = createEntityAdapter<PublicHare>({
  sortComparer: (a, b) => a.number - b.number,
});

export const initialSearchState: SearchState = hareAdapter.getInitialState({
  loadStatus: 'idle',
  instance: null,
  serverProgress: { found: 0, total: 0 },
  locating: false,
  position: null,
  positionError: null,
});

export const searchFeature = createFeature({
  name: 'search',
  reducer: createReducer(
    initialSearchState,
    on(searchPageActions.opened, searchPageActions.retryClicked, (state): SearchState => ({
      ...state,
      loadStatus: 'loading',
    })),
    on(publicInstanceApiActions.loadSucceeded, (state, { instance }): SearchState =>
      hareAdapter.setAll(instance.hares ?? [], {
        ...state,
        loadStatus: 'loaded',
        instance: {
          name: instance.name,
          description: instance.description,
          eventDate: instance.eventDate,
          status: instance.status,
        },
        serverProgress: { found: instance.foundHares, total: instance.totalHares },
      }),
    ),
    on(publicInstanceApiActions.loadFailed, (state, { reason }): SearchState => ({
      ...state,
      loadStatus: reason,
    })),
    on(searchPageActions.locateMeClicked, (state): SearchState => ({
      ...state,
      locating: true,
      positionError: null,
    })),
    on(geolocationActions.positionChanged, (state, { position }): SearchState => ({
      ...state,
      locating: false,
      position,
      positionError: null,
    })),
    on(geolocationActions.failed, (state, { reason }): SearchState => ({
      ...state,
      locating: false,
      positionError: reason,
    })),
    on(searchPageActions.closed, (): SearchState => initialSearchState),
  ),
  extraSelectors: ({ selectSearchState, selectServerProgress, selectInstance }) => {
    const { selectAll } = hareAdapter.getSelectors(selectSearchState);
    const selectHares = createSelector(selectAll, (hares) => hares);
    return {
      selectHares,
      selectHaresVisible: createSelector(
        selectInstance,
        (instance) => instance !== null && instance.status !== 'Draft',
      ),
      /** Counted from the loaded hares, so live updates are reflected; server counts as fallback. */
      selectProgress: createSelector(
        selectHares,
        selectServerProgress,
        (hares, server): Progress =>
          hares.length > 0
            ? { found: hares.filter((h) => h.foundAt !== null).length, total: hares.length }
            : server,
      ),
    };
  },
});
