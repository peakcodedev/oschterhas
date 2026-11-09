import { createActionGroup, emptyProps, props } from '@ngrx/store';
import {
  AddOrganizerRequest,
  AdminHare,
  CreateInstanceRequest,
  GpxImportMode,
  GpxImportResult,
  GpxPreview,
  InstanceDetail,
  InstanceStatus,
  InstanceSummary,
  Organizer,
  SaveHareRequest,
  UpdateInstanceRequest,
  UpdateUserRequest,
  User,
} from '../../../core/api/api-types';

export const adminPageActions = createActionGroup({
  source: 'Admin Page',
  events: {
    instancesOpened: emptyProps(),
    instanceOpened: props<{ instanceId: string }>(),
    instanceClosed: emptyProps(),
    createInstanceSubmitted: props<{ request: CreateInstanceRequest }>(),
    updateInstanceSubmitted: props<{ instanceId: string; request: UpdateInstanceRequest }>(),
    statusChangeRequested: props<{ instanceId: string; status: InstanceStatus }>(),
    deleteInstanceConfirmed: props<{ instanceId: string }>(),
    addOrganizerSubmitted: props<{ instanceId: string; request: AddOrganizerRequest }>(),
    removeOrganizerConfirmed: props<{ instanceId: string; userId: string }>(),
    resendInvitationClicked: props<{ instanceId: string; userId: string }>(),
    hareSelected: props<{ hareId: string | null }>(),
    hareSaveSubmitted: props<{
      instanceId: string;
      hareId: string | null;
      request: SaveHareRequest;
    }>(),
    hareDeleteConfirmed: props<{ instanceId: string; hareId: string }>(),
    hareResetClicked: props<{ instanceId: string; hareId: string }>(),
    gpxFileChosen: props<{ instanceId: string; uploadId: string; fileName: string }>(),
    gpxImportConfirmed: props<{ instanceId: string; uploadId: string; mode: GpxImportMode }>(),
    gpxCleared: emptyProps(),
    usersOpened: emptyProps(),
    userUpdateSubmitted: props<{ userId: string; request: UpdateUserRequest }>(),
    messageDismissed: emptyProps(),
  },
});

export const adminApiActions = createActionGroup({
  source: 'Admin API',
  events: {
    instancesLoaded: props<{ instances: InstanceSummary[] }>(),
    instanceLoaded: props<{ instance: InstanceDetail }>(),
    instanceCreated: props<{ instance: InstanceDetail }>(),
    instanceSaved: props<{ instance: InstanceDetail }>(),
    instanceDeleted: props<{ instanceId: string }>(),
    organizerAdded: props<{ instanceId: string; organizer: Organizer }>(),
    organizerRemoved: props<{ instanceId: string; userId: string }>(),
    invitationSent: emptyProps(),
    haresLoaded: props<{ hares: AdminHare[] }>(),
    hareSaved: props<{ hare: AdminHare; created: boolean }>(),
    hareDeleted: props<{ hareId: string }>(),
    gpxPreviewLoaded: props<{ preview: GpxPreview }>(),
    gpxImported: props<{ instanceId: string; result: GpxImportResult }>(),
    usersLoaded: props<{ users: User[] }>(),
    userSaved: props<{ user: User }>(),
    /** Any failed call; `code` is a stable API error code (see problemCode). */
    failed: props<{ operation: string; code: string }>(),
  },
});
