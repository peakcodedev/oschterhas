import { components } from './api-schema';

/** Generated from the API's OpenAPI document (`npm run api:types`). */
type Schemas = components['schemas'];

export type InstanceStatus = Schemas['InstanceStatus'];
export type PublicInstance = Schemas['PublicInstanceDto'];
export type PublicHare = Schemas['PublicHareDto'];
export type HareFound = Schemas['HareFoundDto'];
export type MarkFoundRequest = Schemas['MarkFoundRequest'];

/** RFC 9457 problem with the API's stable error code. */
export interface ApiProblem {
  status?: number;
  title?: string;
  code?: string;
}
