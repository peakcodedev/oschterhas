import { InjectionToken } from '@angular/core';

/** Browser geolocation; replaceable in tests. Null when the browser has none. */
export const GEOLOCATION = new InjectionToken<Geolocation | null>('GEOLOCATION', {
  providedIn: 'root',
  factory: () =>
    typeof navigator !== 'undefined' && 'geolocation' in navigator ? navigator.geolocation : null,
});
