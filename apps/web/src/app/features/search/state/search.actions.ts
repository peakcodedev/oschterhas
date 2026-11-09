import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { HareFound, PublicInstance } from '../../../core/api/api-types';
import { GeoPosition, LoadFailure, MarkFoundFailure, PositionFailure } from './search.models';

export const searchPageActions = createActionGroup({
  source: 'Search Page',
  events: {
    opened: emptyProps(),
    closed: emptyProps(),
    retryClicked: emptyProps(),
    locateMeClicked: emptyProps(),
    foundDialogOpened: props<{ hareId: string }>(),
    foundDialogClosed: emptyProps(),
    markFoundSubmitted: props<{ hareId: string; nickname: string | null }>(),
  },
});

export const publicInstanceApiActions = createActionGroup({
  source: 'Public Instance API',
  events: {
    loadSucceeded: props<{ instance: PublicInstance }>(),
    loadFailed: props<{ reason: LoadFailure }>(),
    /** A background reload failed; the page keeps showing the last state. */
    reloadFailed: emptyProps(),
    markFoundSucceeded: props<{ found: HareFound }>(),
    markFoundFailed: props<{ hareId: string; reason: MarkFoundFailure }>(),
  },
});

export const instanceEventsActions = createActionGroup({
  source: 'Instance Events',
  events: {
    connected: emptyProps(),
    disconnected: emptyProps(),
    hareFound: props<{ found: HareFound }>(),
    hareReset: props<{ hareId: string }>(),
    /** Something changed that needs a full reload (import, status change, missed events after reconnect). */
    reloadRequested: emptyProps(),
  },
});

export const geolocationActions = createActionGroup({
  source: 'Geolocation',
  events: {
    positionChanged: props<{ position: GeoPosition }>(),
    failed: props<{ reason: PositionFailure }>(),
  },
});
