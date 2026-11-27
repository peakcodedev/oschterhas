import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { CreateInstanceRequest } from '../../../core/api/api-types';
import { authFeature } from '../../../core/auth/auth.reducer';
import { environment } from '../../../../environments/environment';
import { InstanceForm } from '../components/instance-form';
import { StatusBadge } from '../components/status-badge';
import { adminPageActions } from '../state/admin.actions';
import { adminFeature } from '../state/admin.reducer';

@Component({
  selector: 'app-instance-list-page',
  imports: [RouterLink, TranslocoPipe, InstanceForm, StatusBadge],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instance-list-page.html',
})
export class InstanceListPage implements OnInit {
  private readonly store = inject(Store);
  protected readonly instances = this.store.selectSignal(adminFeature.selectInstances);
  protected readonly status = this.store.selectSignal(adminFeature.selectInstancesStatus);
  protected readonly saving = this.store.selectSignal(adminFeature.selectSaving);
  protected readonly user = this.store.selectSignal(authFeature.selectUser);
  protected readonly creating = signal(false);
  protected readonly baseDomain = environment.baseDomain;

  ngOnInit(): void {
    this.store.dispatch(adminPageActions.instancesOpened());
  }

  protected create(request: CreateInstanceRequest): void {
    this.store.dispatch(adminPageActions.createInstanceSubmitted({ request }));
  }
}
