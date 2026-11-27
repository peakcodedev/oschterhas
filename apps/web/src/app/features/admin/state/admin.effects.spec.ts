import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideMockActions } from '@ngrx/effects/testing';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, of, Subject, throwError, toArray, take } from 'rxjs';
import { AdminApiService } from '../../../core/api/admin-api.service';
import { InstanceDetail } from '../../../core/api/api-types';
import { authActions } from '../../../core/auth/auth.actions';
import { adminApiActions, adminPageActions } from './admin.actions';
import {
  addOrganizer,
  importGpx,
  openCreatedInstance,
  previewGpx,
  sessionExpired,
} from './admin.effects';
import { PendingUploads } from './pending-uploads';

const detail = { id: 'i1', name: 'Tann' } as InstanceDetail;

function setup(api: Partial<AdminApiService>, router: Partial<Router> = {}) {
  const actions$ = new Subject<Action>();
  TestBed.configureTestingModule({
    providers: [
      provideMockActions(() => actions$ as Observable<Action>),
      { provide: AdminApiService, useValue: api },
      { provide: Router, useValue: router },
    ],
  });
  return actions$;
}

describe('admin effects', () => {
  it('opens the detail page of a created instance', () => {
    const navigate = vi.fn(() => Promise.resolve(true));
    const actions$ = setup({}, { navigate });
    const subscription = TestBed.runInInjectionContext(() => openCreatedInstance()).subscribe();

    actions$.next(adminApiActions.instanceCreated({ instance: detail }));

    expect(navigate).toHaveBeenCalledWith(['/instanzen', 'i1']);
    subscription.unsubscribe();
  });

  it('previews and imports the chosen file', async () => {
    const file = new File(['<gpx/>'], 'tann.gpx');
    const gpxPreview = vi.fn(() =>
      of({ waypoints: [], warnings: [], existingHares: 0, existingFound: 0, matchingWaypoints: 0 }),
    );
    const gpxImport = vi.fn(() => of({ created: 1, updated: 0, deleted: 0, warnings: [] }));
    const actions$ = setup({ gpxPreview, gpxImport });
    const uploadId = TestBed.inject(PendingUploads).add(file);

    const preview = firstValueFrom(TestBed.runInInjectionContext(() => previewGpx()));
    actions$.next(
      adminPageActions.gpxFileChosen({ instanceId: 'i1', uploadId, fileName: 'tann.gpx' }),
    );
    expect((await preview).type).toBe(adminApiActions.gpxPreviewLoaded.type);

    const imported = firstValueFrom(TestBed.runInInjectionContext(() => importGpx()));
    actions$.next(
      adminPageActions.gpxImportConfirmed({ instanceId: 'i1', uploadId, mode: 'Replace' }),
    );
    expect((await imported).type).toBe(adminApiActions.gpxImported.type);
    expect(gpxImport).toHaveBeenCalledWith('i1', file, 'Replace');
    expect(TestBed.inject(PendingUploads).get(uploadId)).toBeUndefined();
  });

  it('reports a missing upload', async () => {
    const actions$ = setup({});
    const result = firstValueFrom(TestBed.runInInjectionContext(() => importGpx()));

    actions$.next(
      adminPageActions.gpxImportConfirmed({ instanceId: 'i1', uploadId: 'gone', mode: 'Merge' }),
    );

    expect(await result).toEqual(
      adminApiActions.failed({ operation: 'gpxImport', code: 'gpx.file_missing' }),
    );
  });

  it('reloads the instance when the invitation failed after assigning', async () => {
    const actions$ = setup({
      addOrganizer: () =>
        throwError(
          () =>
            new HttpErrorResponse({ status: 500, error: { code: 'organizer.invitation_failed' } }),
        ),
      instance: () => of(detail),
    });
    const result = firstValueFrom(
      TestBed.runInInjectionContext(() => addOrganizer()).pipe(take(2), toArray()),
    );

    actions$.next(
      adminPageActions.addOrganizerSubmitted({
        instanceId: 'i1',
        request: { email: 'a@b.ch', displayName: 'A' },
      }),
    );

    expect(await result).toEqual([
      adminApiActions.failed({ operation: 'addOrganizer', code: 'organizer.invitation_failed' }),
      adminApiActions.instanceLoaded({ instance: detail }),
    ]);
  });

  it('turns 401 answers into an expired session', async () => {
    const actions$ = setup({});
    const result = firstValueFrom(TestBed.runInInjectionContext(() => sessionExpired()));

    actions$.next(adminApiActions.failed({ operation: 'instances', code: 'unauthorized' }));

    expect(await result).toEqual(authActions.sessionExpired());
  });
});
