import { Action } from '@ngrx/store';
import { AdminHare, InstanceDetail, InstanceSummary } from '../../../core/api/api-types';
import { adminApiActions, adminPageActions } from './admin.actions';
import { adminFeature, AdminState, initialAdminState } from './admin.reducer';

const hare = (number: number, found = false): AdminHare => ({
  id: `h${number}`,
  number,
  name: null,
  description: null,
  lat: 47,
  lon: 8,
  elevation: null,
  sourceWaypointId: null,
  foundAt: found ? '2027-03-28T10:00:00Z' : null,
  foundBy: found ? 'Lina' : null,
});

const detail: InstanceDetail = {
  id: 'i1',
  slug: 'tann',
  name: 'Tann',
  description: null,
  eventDate: null,
  status: 'Draft',
  totalHares: 0,
  foundHares: 0,
  publicUrl: 'https://tann.oschterhas.li',
  subdomainUrl: 'https://tann.oschterhas.li',
  customDomain: null,
  customDomainStatus: 'None',
  customDomainVerifiedAt: null,
  customDomainLastCheckAt: null,
  customDomainFailedChecks: 0,
  organizers: [],
};

const summary: InstanceSummary = {
  id: 'i1',
  slug: 'tann',
  name: 'Tann',
  status: 'Draft',
  eventDate: null,
  totalHares: 0,
  foundHares: 0,
  publicUrl: 'https://tann.oschterhas.li',
};

const reduce = (...actions: Action[]): AdminState =>
  actions.reduce(adminFeature.reducer, initialAdminState);
const select = (state: AdminState) => ({ admin: state });

describe('admin reducer', () => {
  it('marks saving while a change runs and shows the result', () => {
    const saving = reduce(
      adminPageActions.statusChangeRequested({ instanceId: 'i1', status: 'Active' }),
    );
    const saved = reduce(
      adminApiActions.instancesLoaded({ instances: [summary] }),
      adminPageActions.statusChangeRequested({ instanceId: 'i1', status: 'Active' }),
      adminApiActions.instanceSaved({ instance: { ...detail, status: 'Active' } }),
    );

    expect(saving.saving).toBe(true);
    expect(saved.saving).toBe(false);
    expect(saved.instances[0].status).toBe('Active');
    expect(saved.message).toEqual({ kind: 'success', key: 'admin.messages.saved' });
  });

  it('shows translated errors and stops saving', () => {
    const state = reduce(
      adminPageActions.createInstanceSubmitted({
        request: { slug: 'tann', name: 'x', description: null, eventDate: null },
      }),
      adminApiActions.failed({ operation: 'createInstance', code: 'instance.slug_taken' }),
    );

    expect(state.saving).toBe(false);
    expect(state.message).toEqual({ kind: 'error', key: 'errors.instance.slug_taken' });
  });

  it('distinguishes a missing instance', () => {
    const state = reduce(
      adminPageActions.instanceOpened({ instanceId: 'x' }),
      adminApiActions.failed({ operation: 'instance', code: 'instance.not_found' }),
    );

    expect(state.detailStatus).toBe('notFound');
  });

  it('opening another instance clears hares, selection and GPX state', () => {
    const state = reduce(
      adminApiActions.instanceLoaded({ instance: detail }),
      adminApiActions.haresLoaded({ hares: [hare(1)] }),
      adminPageActions.hareSelected({ hareId: 'h1' }),
      adminPageActions.instanceOpened({ instanceId: 'other' }),
    );

    expect(state.ids).toEqual([]);
    expect(state.selectedHareId).toBeNull();
    expect(state.detail).toBeNull();
  });

  it('upserts saved hares and selects new ones', () => {
    const state = reduce(
      adminApiActions.haresLoaded({ hares: [hare(2, true), hare(1)] }),
      adminApiActions.hareSaved({ hare: hare(3), created: true }),
    );

    expect(adminFeature.selectHares(select(state)).map((h) => h.number)).toEqual([1, 2, 3]);
    expect(adminFeature.selectSelectedHare(select(state))?.id).toBe('h3');
    expect(adminFeature.selectHareProgress(select(state))).toEqual({ found: 1, total: 3 });
  });

  it('walks through the GPX import', () => {
    const preview = {
      waypoints: [],
      warnings: [],
      existingHares: 0,
      existingFound: 0,
      matchingWaypoints: 0,
    };
    const chosen = reduce(
      adminPageActions.gpxFileChosen({ instanceId: 'i1', uploadId: 'u1', fileName: 'a.gpx' }),
    );
    const previewed = reduce(
      adminPageActions.gpxFileChosen({ instanceId: 'i1', uploadId: 'u1', fileName: 'a.gpx' }),
      adminApiActions.gpxPreviewLoaded({ preview }),
    );
    const failed = reduce(
      adminPageActions.gpxFileChosen({ instanceId: 'i1', uploadId: 'u1', fileName: 'a.gpx' }),
      adminApiActions.failed({ operation: 'gpxPreview', code: 'gpx.not_gpx' }),
    );
    const done = reduce(
      adminPageActions.gpxFileChosen({ instanceId: 'i1', uploadId: 'u1', fileName: 'a.gpx' }),
      adminApiActions.gpxPreviewLoaded({ preview }),
      adminPageActions.gpxImportConfirmed({ instanceId: 'i1', uploadId: 'u1', mode: 'Merge' }),
      adminApiActions.gpxImported({
        instanceId: 'i1',
        result: { created: 1, updated: 0, deleted: 0, warnings: [] },
      }),
    );

    expect(chosen.gpx).toMatchObject({ status: 'previewing', uploadId: 'u1', fileName: 'a.gpx' });
    expect(previewed.gpx.status).toBe('preview');
    expect(failed.gpx.status).toBe('idle');
    expect(failed.message?.key).toBe('errors.gpx.not_gpx');
    expect(done.gpx).toMatchObject({ status: 'done', uploadId: null });
  });

  it('removes organizers and deleted instances', () => {
    const organizer = { userId: 'o1', email: 'o@test', displayName: 'O', invitationPending: true };
    const added = reduce(
      adminApiActions.instanceLoaded({ instance: detail }),
      adminApiActions.organizerAdded({ instanceId: 'i1', organizer }),
    );
    const removed = reduce(
      adminApiActions.instanceLoaded({ instance: { ...detail, organizers: [organizer] } }),
      adminApiActions.organizerRemoved({ instanceId: 'i1', userId: 'o1' }),
    );
    const deleted = reduce(
      adminApiActions.instancesLoaded({ instances: [summary] }),
      adminApiActions.instanceDeleted({ instanceId: 'i1' }),
    );

    expect(added.detail?.organizers).toEqual([organizer]);
    expect(added.message?.key).toBe('admin.messages.organizerInvited');
    expect(removed.detail?.organizers).toEqual([]);
    expect(deleted.instances).toEqual([]);
  });

  it('shows the outcome of a custom domain check', () => {
    const verified = reduce(
      adminPageActions.customDomainVerifyClicked({ instanceId: 'i1' }),
      adminApiActions.customDomainChecked({
        check: {
          instance: {
            ...detail,
            customDomain: 'ostern.example.ch',
            customDomainStatus: 'Verified',
          },
          outcome: 'Verified',
        },
      }),
    );
    const pending = reduce(
      adminApiActions.customDomainChecked({
        check: {
          instance: { ...detail, customDomain: 'ostern.example.ch', customDomainStatus: 'Pending' },
          outcome: 'Unreachable',
        },
      }),
    );

    expect(verified.saving).toBe(false);
    expect(verified.detail?.customDomainStatus).toBe('Verified');
    expect(verified.message).toEqual({
      kind: 'success',
      key: 'admin.customDomain.outcome.Verified',
    });
    expect(pending.message).toEqual({
      kind: 'error',
      key: 'admin.customDomain.outcome.Unreachable',
    });
  });
});
