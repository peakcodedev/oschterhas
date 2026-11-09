import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { GpxImportMode } from '../../../core/api/api-types';
import { adminPageActions } from '../state/admin.actions';
import { GpxState } from '../state/admin.models';
import { PendingUploads } from '../state/pending-uploads';

/** Upload a GPX file, check the preview, then replace or merge the hares. */
@Component({
  selector: 'app-gpx-import-section',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './gpx-import-section.html',
})
export class GpxImportSection {
  readonly instanceId = input.required<string>();
  readonly gpx = input.required<GpxState>();

  private readonly store = inject(Store);
  private readonly uploads = inject(PendingUploads);
  private readonly transloco = inject(TranslocoService);
  protected readonly mode = signal<GpxImportMode>('Merge');

  protected choose(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }
    const uploadId = this.uploads.add(file);
    this.store.dispatch(
      adminPageActions.gpxFileChosen({
        instanceId: this.instanceId(),
        uploadId,
        fileName: file.name,
      }),
    );
  }

  protected import(): void {
    const { uploadId, preview } = this.gpx();
    if (!uploadId) {
      return;
    }
    const losesFound = this.mode() === 'Replace' && (preview?.existingFound ?? 0) > 0;
    if (
      losesFound &&
      !confirm(
        this.transloco.translate('admin.gpx.confirmReplaceFound', {
          count: preview?.existingFound,
        }),
      )
    ) {
      return;
    }
    this.store.dispatch(
      adminPageActions.gpxImportConfirmed({
        instanceId: this.instanceId(),
        uploadId,
        mode: this.mode(),
      }),
    );
  }

  protected clear(): void {
    this.store.dispatch(adminPageActions.gpxCleared());
  }
}
