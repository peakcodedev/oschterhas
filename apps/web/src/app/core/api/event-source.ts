import { InjectionToken } from '@angular/core';

/**
 * Live updates of the current instance (server-sent events). `ngsw-bypass` keeps the long-lived stream
 * out of the Angular service worker (EventSource cannot set the bypass header).
 */
export const PUBLIC_EVENTS_URL = '/api/public/events?ngsw-bypass=true';

export type EventSourceFactory = (url: string) => EventSource;

/** Creates the browser EventSource; replaceable in tests. */
export const EVENT_SOURCE_FACTORY = new InjectionToken<EventSourceFactory>('EVENT_SOURCE_FACTORY', {
  providedIn: 'root',
  factory: () => (url: string) => new EventSource(url),
});
