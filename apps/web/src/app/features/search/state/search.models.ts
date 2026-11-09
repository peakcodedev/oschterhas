import { InstanceStatus } from '../../../core/api/api-types';

export type LoadStatus = 'idle' | 'loading' | 'loaded' | 'notFound' | 'error';
export type LoadFailure = 'notFound' | 'error';
export type PositionFailure = 'denied' | 'unavailable' | 'unsupported';
export type MarkFoundFailure =
  'alreadyFound' | 'notActive' | 'rateLimited' | 'invalidName' | 'error';
export type LiveStatus = 'connecting' | 'connected' | 'disconnected';

export interface GeoPosition {
  lat: number;
  lon: number;
  /** Accuracy radius in metres. */
  accuracy: number;
}

export interface InstanceInfo {
  name: string;
  description: string | null;
  eventDate: string | null;
  status: InstanceStatus;
}

export interface Progress {
  found: number;
  total: number;
}

export interface FoundDialogState {
  hareId: string | null;
  status: 'closed' | 'open' | 'submitting' | 'failed';
  error: MarkFoundFailure | null;
}
