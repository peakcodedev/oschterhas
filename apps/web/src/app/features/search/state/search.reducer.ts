import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { HareFound, PublicHare } from '../../../core/api/api-types';
import {
  geolocationActions,
  instanceEventsActions,
  publicInstanceApiActions,
  searchPageActions,
} from './search.actions';
import {
  FoundDialogState,
  GeoPosition,
  InstanceInfo,
  LiveStatus,
  LoadStatus,
  PositionFailure,
  Progress,
} from './search.models';

export interface SearchState extends EntityState<PublicHare> {
  loadStatus: LoadStatus;
  instance: InstanceInfo | null;
  /** Counts from the server; used while hares are hidden (draft). */
  serverProgress: Progress;
  locating: boolean;
  position: GeoPosition | null;
  positionError: PositionFailure | null;
  foundDialog: FoundDialogState;
  /** Last nickname entered, offered again in the next dialog. */
  nickname: string | null;
  live: LiveStatus;
}

export const hareAdapter = createEntityAdapter<PublicHare>({
  sortComparer: (a, b) => a.number - b.number,
});

const closedDialog: FoundDialogState = { hareId: null, status: 'closed', error: null };

export const initialSearchState: SearchState = hareAdapter.getInitialState({
  loadStatus: 'idle',
  instance: null,
  serverProgress: { found: 0, total: 0 },
  locating: false,
  position: null,
  positionError: null,
  foundDialog: closedDialog,
  nickname: null,
  live: 'connecting',
});

const applyFound = (state: SearchState, { hareId, foundAt, foundBy }: HareFound): SearchState =>
  hareAdapter.updateOne({ id: hareId, changes: { foundAt, foundBy } }, state);

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
    on(searchPageActions.foundDialogOpened, (state, { hareId }): SearchState => ({
      ...state,
      foundDialog: { hareId, status: 'open', error: null },
    })),
    on(searchPageActions.foundDialogClosed, (state): SearchState => ({
      ...state,
      foundDialog: closedDialog,
    })),
    on(searchPageActions.markFoundSubmitted, (state, { hareId, nickname }): SearchState => ({
      ...state,
      nickname,
      foundDialog: { hareId, status: 'submitting', error: null },
    })),
    on(publicInstanceApiActions.markFoundSucceeded, (state, { found }): SearchState => ({
      ...applyFound(state, found),
      foundDialog: closedDialog,
    })),
    on(publicInstanceApiActions.markFoundFailed, (state, { hareId, reason }): SearchState => ({
      ...state,
      foundDialog: { hareId, status: 'failed', error: reason },
    })),
    on(instanceEventsActions.hareFound, (state, { found }): SearchState =>
      applyFound(state, found),
    ),
    on(instanceEventsActions.hareReset, (state, { hareId }): SearchState =>
      hareAdapter.updateOne({ id: hareId, changes: { foundAt: null, foundBy: null } }, state),
    ),
    on(instanceEventsActions.connected, (state): SearchState => ({ ...state, live: 'connected' })),
    on(instanceEventsActions.disconnected, (state): SearchState => ({
      ...state,
      live: 'disconnected',
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
  extraSelectors: ({
    selectSearchState,
    selectServerProgress,
    selectInstance,
    selectFoundDialog,
  }) => {
    const { selectAll, selectEntities } = hareAdapter.getSelectors(selectSearchState);
    const selectHares = createSelector(selectAll, (hares) => hares);
    return {
      selectHares,
      selectHaresVisible: createSelector(
        selectInstance,
        (instance) => instance !== null && instance.status !== 'Draft',
      ),
      selectCanMarkFound: createSelector(
        selectInstance,
        (instance) => instance?.status === 'Active',
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
      selectDialogHare: createSelector(selectFoundDialog, selectEntities, (dialog, entities) =>
        dialog.hareId ? (entities[dialog.hareId] ?? null) : null,
      ),
    };
  },
});
