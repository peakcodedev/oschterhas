import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { AdminHare, InstanceDetail, InstanceSummary, User } from '../../../core/api/api-types';
import { adminApiActions, adminPageActions } from './admin.actions';
import { AdminMessage, GpxState, LoadStatus } from './admin.models';

export interface AdminState extends EntityState<AdminHare> {
  instances: InstanceSummary[];
  instancesStatus: LoadStatus;
  detail: InstanceDetail | null;
  detailStatus: LoadStatus | 'notFound';
  selectedHareId: string | null;
  gpx: GpxState;
  users: User[];
  usersStatus: LoadStatus;
  /** A change is being saved; forms disable their submit button. */
  saving: boolean;
  message: AdminMessage | null;
}

export const adminHareAdapter = createEntityAdapter<AdminHare>({
  sortComparer: (a, b) => a.number - b.number,
});

export const initialGpxState: GpxState = {
  status: 'idle',
  uploadId: null,
  fileName: null,
  preview: null,
  result: null,
};

export const initialAdminState: AdminState = adminHareAdapter.getInitialState({
  instances: [],
  instancesStatus: 'idle',
  detail: null,
  detailStatus: 'idle',
  selectedHareId: null,
  gpx: initialGpxState,
  users: [],
  usersStatus: 'idle',
  saving: false,
  message: null,
});

const success = (key: string): AdminMessage => ({ kind: 'success', key });

const savingActions = [
  adminPageActions.createInstanceSubmitted,
  adminPageActions.updateInstanceSubmitted,
  adminPageActions.statusChangeRequested,
  adminPageActions.deleteInstanceConfirmed,
  adminPageActions.addOrganizerSubmitted,
  adminPageActions.removeOrganizerConfirmed,
  adminPageActions.resendInvitationClicked,
  adminPageActions.hareSaveSubmitted,
  adminPageActions.hareDeleteConfirmed,
  adminPageActions.hareResetClicked,
  adminPageActions.userUpdateSubmitted,
  adminPageActions.customDomainSubmitted,
  adminPageActions.customDomainVerifyClicked,
  adminPageActions.customDomainRemoveConfirmed,
] as const;

export const adminFeature = createFeature({
  name: 'admin',
  reducer: createReducer(
    initialAdminState,
    on(...savingActions, (state): AdminState => ({ ...state, saving: true, message: null })),
    on(adminPageActions.messageDismissed, (state): AdminState => ({ ...state, message: null })),

    on(adminPageActions.instancesOpened, (state): AdminState => ({
      ...state,
      instancesStatus: 'loading',
    })),
    on(adminApiActions.instancesLoaded, (state, { instances }): AdminState => ({
      ...state,
      instances,
      instancesStatus: 'loaded',
    })),

    on(adminPageActions.instanceOpened, (state, { instanceId }): AdminState =>
      adminHareAdapter.removeAll({
        ...state,
        detail: state.detail?.id === instanceId ? state.detail : null,
        detailStatus: 'loading',
        selectedHareId: null,
        gpx: initialGpxState,
      }),
    ),
    on(adminPageActions.instanceClosed, (state): AdminState =>
      adminHareAdapter.removeAll({
        ...state,
        detail: null,
        detailStatus: 'idle',
        selectedHareId: null,
        gpx: initialGpxState,
      }),
    ),
    on(adminApiActions.instanceLoaded, (state, { instance }): AdminState => ({
      ...state,
      detail: instance,
      detailStatus: 'loaded',
    })),
    on(adminApiActions.instanceCreated, (state, { instance }): AdminState => ({
      ...state,
      saving: false,
      detail: instance,
      message: success('admin.messages.instanceCreated'),
    })),
    on(adminApiActions.instanceSaved, (state, { instance }): AdminState => ({
      ...state,
      saving: false,
      detail: instance,
      instances: state.instances.map((i) =>
        i.id === instance.id
          ? {
              ...i,
              name: instance.name,
              slug: instance.slug,
              status: instance.status,
              eventDate: instance.eventDate,
              publicUrl: instance.publicUrl,
            }
          : i,
      ),
      message: success('admin.messages.saved'),
    })),
    on(adminApiActions.instanceDeleted, (state, { instanceId }): AdminState => ({
      ...state,
      saving: false,
      detail: null,
      detailStatus: 'idle',
      instances: state.instances.filter((i) => i.id !== instanceId),
      message: success('admin.messages.instanceDeleted'),
    })),

    on(adminApiActions.organizerAdded, (state, { organizer }): AdminState => ({
      ...state,
      saving: false,
      detail: state.detail
        ? { ...state.detail, organizers: [...state.detail.organizers, organizer] }
        : null,
      message: success(
        organizer.invitationPending
          ? 'admin.messages.organizerInvited'
          : 'admin.messages.organizerAdded',
      ),
    })),
    on(adminApiActions.organizerRemoved, (state, { userId }): AdminState => ({
      ...state,
      saving: false,
      detail: state.detail
        ? {
            ...state.detail,
            organizers: state.detail.organizers.filter((o) => o.userId !== userId),
          }
        : null,
      message: success('admin.messages.organizerRemoved'),
    })),
    on(adminApiActions.invitationSent, (state): AdminState => ({
      ...state,
      saving: false,
      message: success('admin.messages.invitationSent'),
    })),

    on(adminApiActions.haresLoaded, (state, { hares }): AdminState =>
      adminHareAdapter.setAll(hares, state),
    ),
    on(adminPageActions.hareSelected, (state, { hareId }): AdminState => ({
      ...state,
      selectedHareId: hareId,
    })),
    on(adminApiActions.hareSaved, (state, { hare, created }): AdminState =>
      adminHareAdapter.upsertOne(hare, {
        ...state,
        saving: false,
        selectedHareId: created ? hare.id : state.selectedHareId,
        message: success('admin.messages.saved'),
      }),
    ),
    on(adminApiActions.hareDeleted, (state, { hareId }): AdminState =>
      adminHareAdapter.removeOne(hareId, {
        ...state,
        saving: false,
        selectedHareId: state.selectedHareId === hareId ? null : state.selectedHareId,
        message: success('admin.messages.hareDeleted'),
      }),
    ),

    on(adminPageActions.gpxFileChosen, (state, { uploadId, fileName }): AdminState => ({
      ...state,
      message: null,
      gpx: { ...initialGpxState, status: 'previewing', uploadId, fileName },
    })),
    on(adminApiActions.gpxPreviewLoaded, (state, { preview }): AdminState => ({
      ...state,
      gpx: { ...state.gpx, status: 'preview', preview },
    })),
    on(adminPageActions.gpxImportConfirmed, (state): AdminState => ({
      ...state,
      message: null,
      gpx: { ...state.gpx, status: 'importing' },
    })),
    on(adminApiActions.gpxImported, (state, { result }): AdminState => ({
      ...state,
      gpx: { ...initialGpxState, status: 'done', result },
      message: success('admin.messages.gpxImported'),
    })),
    on(adminPageActions.gpxCleared, (state): AdminState => ({ ...state, gpx: initialGpxState })),

    on(adminApiActions.customDomainChecked, (state, { check }): AdminState => ({
      ...state,
      saving: false,
      detail: check.instance,
      message: {
        kind: check.outcome === 'Verified' ? 'success' : 'error',
        key: `admin.customDomain.outcome.${check.outcome}`,
      },
    })),

    on(adminPageActions.usersOpened, (state): AdminState => ({ ...state, usersStatus: 'loading' })),
    on(adminApiActions.usersLoaded, (state, { users }): AdminState => ({
      ...state,
      users,
      usersStatus: 'loaded',
    })),
    on(adminApiActions.userSaved, (state, { user }): AdminState => ({
      ...state,
      saving: false,
      users: state.users.map((u) => (u.id === user.id ? user : u)),
      message: success('admin.messages.saved'),
    })),

    on(adminApiActions.failed, (state, { operation, code }): AdminState => ({
      ...state,
      saving: false,
      instancesStatus: operation === 'instances' ? 'error' : state.instancesStatus,
      detailStatus:
        operation === 'instance'
          ? code === 'instance.not_found'
            ? 'notFound'
            : 'error'
          : state.detailStatus,
      usersStatus: operation === 'users' ? 'error' : state.usersStatus,
      gpx:
        operation === 'gpxPreview' || operation === 'gpxImport'
          ? { ...state.gpx, status: state.gpx.preview ? 'preview' : 'idle' }
          : state.gpx,
      message: { kind: 'error', key: `errors.${code}` },
    })),
  ),
  extraSelectors: ({ selectAdminState, selectSelectedHareId }) => {
    const { selectAll, selectEntities } = adminHareAdapter.getSelectors(selectAdminState);
    return {
      selectHares: selectAll,
      selectSelectedHare: createSelector(selectEntities, selectSelectedHareId, (entities, id) =>
        id ? (entities[id] ?? null) : null,
      ),
      selectHareProgress: createSelector(selectAll, (hares) => ({
        found: hares.filter((h) => h.foundAt !== null).length,
        total: hares.length,
      })),
    };
  },
});
