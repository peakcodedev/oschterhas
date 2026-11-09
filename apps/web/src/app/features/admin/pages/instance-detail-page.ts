import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  OnDestroy,
  effect,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { CreateInstanceRequest, InstanceStatus } from '../../../core/api/api-types';
import { authFeature } from '../../../core/auth/auth.reducer';
import { environment } from '../../../../environments/environment';
import { GpxImportSection } from '../components/gpx-import-section';
import { HaresSection } from '../components/hares-section';
import { InstanceForm } from '../components/instance-form';
import { OrganizersSection } from '../components/organizers-section';
import { QrPoster } from '../components/qr-poster';
import { StatusBadge } from '../components/status-badge';
import { adminPageActions } from '../state/admin.actions';
import { adminFeature } from '../state/admin.reducer';

@Component({
  selector: 'app-instance-detail-page',
  imports: [
    RouterLink,
    TranslocoPipe,
    StatusBadge,
    InstanceForm,
    QrPoster,
    OrganizersSection,
    HaresSection,
    GpxImportSection,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instance-detail-page.html',
  styleUrl: './instance-detail-page.scss',
})
export class InstanceDetailPage implements OnDestroy {
  /** Route parameter (component input binding). */
  readonly instanceId = input.required<string>();

  private readonly store = inject(Store);
  private readonly transloco = inject(TranslocoService);
  protected readonly instance = this.store.selectSignal(adminFeature.selectDetail);
  protected readonly status = this.store.selectSignal(adminFeature.selectDetailStatus);
  protected readonly hares = this.store.selectSignal(adminFeature.selectHares);
  protected readonly selectedHare = this.store.selectSignal(adminFeature.selectSelectedHare);
  protected readonly progress = this.store.selectSignal(adminFeature.selectHareProgress);
  protected readonly gpx = this.store.selectSignal(adminFeature.selectGpx);
  protected readonly saving = this.store.selectSignal(adminFeature.selectSaving);
  protected readonly user = this.store.selectSignal(authFeature.selectUser);
  protected readonly editing = signal(false);
  protected readonly baseDomain = environment.baseDomain;

  constructor() {
    effect(() =>
      this.store.dispatch(adminPageActions.instanceOpened({ instanceId: this.instanceId() })),
    );
  }

  ngOnDestroy(): void {
    this.store.dispatch(adminPageActions.instanceClosed());
  }

  protected setStatus(status: InstanceStatus): void {
    const key =
      status === 'Active'
        ? 'admin.detail.confirmStart'
        : status === 'Ended'
          ? 'admin.detail.confirmEnd'
          : 'admin.detail.confirmDraft';
    if (confirm(this.transloco.translate(key))) {
      this.store.dispatch(
        adminPageActions.statusChangeRequested({ instanceId: this.instanceId(), status }),
      );
    }
  }

  protected save(request: CreateInstanceRequest): void {
    this.store.dispatch(
      adminPageActions.updateInstanceSubmitted({ instanceId: this.instanceId(), request }),
    );
    this.editing.set(false);
  }

  protected remove(): void {
    const name = this.instance()?.name ?? '';
    if (confirm(this.transloco.translate('admin.detail.confirmDelete', { name }))) {
      this.store.dispatch(
        adminPageActions.deleteInstanceConfirmed({ instanceId: this.instanceId() }),
      );
    }
  }

  protected print(): void {
    window.print();
  }

  protected async copy(url: string): Promise<void> {
    await navigator.clipboard?.writeText(url);
  }
}
