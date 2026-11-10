import { components } from './api-schema';

/** Generated from the API's OpenAPI document (`npm run api:types`). */
type Schemas = components['schemas'];

export type InstanceStatus = Schemas['InstanceStatus'];
export type PublicInstance = Schemas['PublicInstanceDto'];
export type PublicHare = Schemas['PublicHareDto'];
export type HareFound = Schemas['HareFoundDto'];
export type MarkFoundRequest = Schemas['MarkFoundRequest'];

export type Me = Schemas['MeDto'];
export type InstanceSummary = Schemas['InstanceSummaryDto'];
export type InstanceDetail = Schemas['InstanceDetailDto'];
export type Organizer = Schemas['OrganizerDto'];
export type CreateInstanceRequest = Schemas['CreateInstanceRequest'];
export type UpdateInstanceRequest = Schemas['UpdateInstanceRequest'];
export type SlugCheck = Schemas['SlugCheckDto'];
export type AddOrganizerRequest = Schemas['AddOrganizerRequest'];
export type AdminHare = Schemas['AdminHareDto'];
export type SaveHareRequest = Schemas['SaveHareRequest'];
export type GpxImportMode = Schemas['GpxImportMode'];
export type GpxPreview = Schemas['GpxPreviewDto'];
export type GpxWaypoint = Schemas['GpxWaypoint'];
export type GpxImportResult = Schemas['GpxImportResultDto'];
export type User = Schemas['UserDto'];
export type UpdateUserRequest = Schemas['UpdateUserRequest'];
export type CustomDomainStatus = Schemas['CustomDomainStatus'];
export type CustomDomainCheck = Schemas['CustomDomainCheckDto'];
export type DomainCheckOutcome = Schemas['DomainCheckOutcome'];

/** RFC 9457 problem with the API's stable error code. */
export interface ApiProblem {
  status?: number;
  title?: string;
  code?: string;
}
