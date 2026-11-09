// Only effects may be exported from this file: app.routes.ts passes the whole module to provideEffects().
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import {
  catchError,
  concat,
  concatMap,
  EMPTY,
  exhaustMap,
  filter,
  map,
  mergeMap,
  Observable,
  of,
  switchMap,
  tap,
} from 'rxjs';
import { AdminApiService } from '../../../core/api/admin-api.service';
import { problemCode } from '../../../core/api/problem-code';
import { authActions } from '../../../core/auth/auth.actions';
import { adminApiActions, adminPageActions } from './admin.actions';
import { PendingUploads } from './pending-uploads';

/** Runs an API call; failures become `adminApiActions.failed` with the error code. */
function call<T>(
  operation: string,
  request: Observable<T>,
  onSuccess: (value: T) => Action | Action[],
) {
  return request.pipe(
    mergeMap((value) => {
      const result = onSuccess(value);
      return Array.isArray(result) ? result : [result];
    }),
    catchError((error: unknown) =>
      of(adminApiActions.failed({ operation, code: problemCode(error) })),
    ),
  );
}

export const loadInstances = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(
        adminPageActions.instancesOpened,
        adminApiActions.instanceCreated,
        adminApiActions.instanceDeleted,
      ),
      switchMap(() =>
        call('instances', api.instances(), (instances) =>
          adminApiActions.instancesLoaded({ instances }),
        ),
      ),
    ),
  { functional: true },
);

export const loadInstance = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.instanceOpened),
      switchMap(({ instanceId }) =>
        call('instance', api.instance(instanceId), (instance) =>
          adminApiActions.instanceLoaded({ instance }),
        ),
      ),
    ),
  { functional: true },
);

export const loadHares = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.instanceOpened, adminApiActions.gpxImported),
      switchMap(({ instanceId }) =>
        call('hares', api.hares(instanceId), (hares) => adminApiActions.haresLoaded({ hares })),
      ),
    ),
  { functional: true },
);

/** Counts on the detail page change after an import. */
export const reloadInstanceAfterImport = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminApiActions.gpxImported),
      switchMap(({ instanceId }) =>
        call('instance', api.instance(instanceId), (instance) =>
          adminApiActions.instanceLoaded({ instance }),
        ),
      ),
    ),
  { functional: true },
);

export const createInstance = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.createInstanceSubmitted),
      exhaustMap(({ request }) =>
        call('createInstance', api.createInstance(request), (instance) =>
          adminApiActions.instanceCreated({ instance }),
        ),
      ),
    ),
  { functional: true },
);

export const openCreatedInstance = createEffect(
  (actions$ = inject(Actions), router = inject(Router)) =>
    actions$.pipe(
      ofType(adminApiActions.instanceCreated),
      tap(({ instance }) => void router.navigate(['/instanzen', instance.id])),
    ),
  { functional: true, dispatch: false },
);

export const updateInstance = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.updateInstanceSubmitted),
      exhaustMap(({ instanceId, request }) =>
        call('updateInstance', api.updateInstance(instanceId, request), (instance) =>
          adminApiActions.instanceSaved({ instance }),
        ),
      ),
    ),
  { functional: true },
);

export const setStatus = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.statusChangeRequested),
      exhaustMap(({ instanceId, status }) =>
        call('setStatus', api.setStatus(instanceId, status), (instance) =>
          adminApiActions.instanceSaved({ instance }),
        ),
      ),
    ),
  { functional: true },
);

export const deleteInstance = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.deleteInstanceConfirmed),
      exhaustMap(({ instanceId }) =>
        call('deleteInstance', api.deleteInstance(instanceId), () =>
          adminApiActions.instanceDeleted({ instanceId }),
        ),
      ),
    ),
  { functional: true },
);

export const leaveDeletedInstance = createEffect(
  (actions$ = inject(Actions), router = inject(Router)) =>
    actions$.pipe(
      ofType(adminApiActions.instanceDeleted),
      tap(() => void router.navigate(['/instanzen'])),
    ),
  { functional: true, dispatch: false },
);

export const addOrganizer = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.addOrganizerSubmitted),
      exhaustMap(({ instanceId, request }) =>
        api.addOrganizer(instanceId, request).pipe(
          map((organizer) => adminApiActions.organizerAdded({ instanceId, organizer })),
          catchError((error: unknown) => {
            const failed = adminApiActions.failed({
              operation: 'addOrganizer',
              code: problemCode(error),
            });
            // The organizer was assigned although the invitation failed: reload to show the real state.
            return failed.code === 'organizer.invitation_failed'
              ? concat(
                  of(failed),
                  api.instance(instanceId).pipe(
                    map((instance) => adminApiActions.instanceLoaded({ instance })),
                    catchError(() => EMPTY),
                  ),
                )
              : of(failed);
          }),
        ),
      ),
    ),
  { functional: true },
);

export const removeOrganizer = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.removeOrganizerConfirmed),
      exhaustMap(({ instanceId, userId }) =>
        call('removeOrganizer', api.removeOrganizer(instanceId, userId), () =>
          adminApiActions.organizerRemoved({ instanceId, userId }),
        ),
      ),
    ),
  { functional: true },
);

export const resendInvitation = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.resendInvitationClicked),
      exhaustMap(({ instanceId, userId }) =>
        call('resendInvitation', api.resendInvitation(instanceId, userId), () =>
          adminApiActions.invitationSent(),
        ),
      ),
    ),
  { functional: true },
);

export const saveHare = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.hareSaveSubmitted),
      concatMap(({ instanceId, hareId, request }) =>
        call(
          'saveHare',
          hareId
            ? api.updateHare(instanceId, hareId, request)
            : api.createHare(instanceId, request),
          (hare) => adminApiActions.hareSaved({ hare, created: hareId === null }),
        ),
      ),
    ),
  { functional: true },
);

export const deleteHare = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.hareDeleteConfirmed),
      concatMap(({ instanceId, hareId }) =>
        call('deleteHare', api.deleteHare(instanceId, hareId), () =>
          adminApiActions.hareDeleted({ hareId }),
        ),
      ),
    ),
  { functional: true },
);

export const resetHare = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.hareResetClicked),
      concatMap(({ instanceId, hareId }) =>
        call('resetHare', api.resetHare(instanceId, hareId), (hare) =>
          adminApiActions.hareSaved({ hare, created: false }),
        ),
      ),
    ),
  { functional: true },
);

export const previewGpx = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService), uploads = inject(PendingUploads)) =>
    actions$.pipe(
      ofType(adminPageActions.gpxFileChosen),
      switchMap(({ instanceId, uploadId }) => {
        const file = uploads.get(uploadId);
        return file
          ? call('gpxPreview', api.gpxPreview(instanceId, file), (preview) =>
              adminApiActions.gpxPreviewLoaded({ preview }),
            )
          : of(adminApiActions.failed({ operation: 'gpxPreview', code: 'gpx.file_missing' }));
      }),
    ),
  { functional: true },
);

export const importGpx = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService), uploads = inject(PendingUploads)) =>
    actions$.pipe(
      ofType(adminPageActions.gpxImportConfirmed),
      exhaustMap(({ instanceId, uploadId, mode }) => {
        const file = uploads.get(uploadId);
        if (!file) {
          return of(adminApiActions.failed({ operation: 'gpxImport', code: 'gpx.file_missing' }));
        }
        return call('gpxImport', api.gpxImport(instanceId, file, mode), (result) => {
          uploads.remove(uploadId);
          return adminApiActions.gpxImported({ instanceId, result });
        });
      }),
    ),
  { functional: true },
);

export const sessionExpired = createEffect(
  (actions$ = inject(Actions)) =>
    actions$.pipe(
      ofType(adminApiActions.failed),
      filter(({ code }) => code === 'unauthorized'),
      map(() => authActions.sessionExpired()),
    ),
  { functional: true },
);

export const loadUsers = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.usersOpened),
      switchMap(() =>
        call('users', api.users(), (users) => adminApiActions.usersLoaded({ users })),
      ),
    ),
  { functional: true },
);

export const updateUser = createEffect(
  (actions$ = inject(Actions), api = inject(AdminApiService)) =>
    actions$.pipe(
      ofType(adminPageActions.userUpdateSubmitted),
      exhaustMap(({ userId, request }) =>
        call('updateUser', api.updateUser(userId, request), (user) =>
          adminApiActions.userSaved({ user }),
        ),
      ),
    ),
  { functional: true },
);
