import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { PublicInstance } from '../../../core/api/api-types';
import { GeoPosition, LoadFailure, PositionFailure } from './search.models';

export const searchPageActions = createActionGroup({
  source: 'Search Page',
  events: {
    opened: emptyProps(),
    closed: emptyProps(),
    retryClicked: emptyProps(),
    locateMeClicked: emptyProps(),
  },
});

export const publicInstanceApiActions = createActionGroup({
  source: 'Public Instance API',
  events: {
    loadSucceeded: props<{ instance: PublicInstance }>(),
    loadFailed: props<{ reason: LoadFailure }>(),
  },
});

export const geolocationActions = createActionGroup({
  source: 'Geolocation',
  events: {
    positionChanged: props<{ position: GeoPosition }>(),
    failed: props<{ reason: PositionFailure }>(),
  },
});
