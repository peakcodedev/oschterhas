import { InjectionToken } from '@angular/core';

/** Live updates of the current instance (server-sent events). */
export const PUBLIC_EVENTS_URL = '/api/public/events';

export type EventSourceFactory = (url: string) => EventSource;

/** Creates the browser EventSource; replaceable in tests. */
export const EVENT_SOURCE_FACTORY = new InjectionToken<EventSourceFactory>('EVENT_SOURCE_FACTORY', {
  providedIn: 'root',
  factory: () => (url: string) => new EventSource(url),
});
