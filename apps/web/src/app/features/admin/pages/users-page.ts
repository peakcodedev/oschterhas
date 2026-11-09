import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { User } from '../../../core/api/api-types';
import { authFeature } from '../../../core/auth/auth.reducer';
import { adminPageActions } from '../state/admin.actions';
import { adminFeature } from '../state/admin.reducer';

/** Accounts that have access to Oschterhas (platform admins only). */
@Component({
  selector: 'app-users-page',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1>{{ 'admin.users.title' | transloco }}</h1>
    @switch (status()) {
      @case ('loaded') {
        <table class="table card">
          <thead>
            <tr>
              <th scope="col">{{ 'admin.users.name' | transloco }}</th>
              <th scope="col">{{ 'admin.users.admin' | transloco }}</th>
              <th scope="col">{{ 'admin.users.active' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @for (user of users(); track user.id) {
              <tr>
                <td>
                  <strong>{{ user.displayName }}</strong>
                  <span class="list__meta">{{ user.email }}</span>
                  @if (user.invitationPending) {
                    <span class="tag">{{ 'admin.organizers.pending' | transloco }}</span>
                  }
                </td>
                <td>
                  <input
                    type="checkbox"
                    class="table__checkbox"
                    [checked]="user.isPlatformAdmin"
                    [disabled]="saving() || user.id === me()?.id"
                    [attr.aria-label]="
                      'admin.users.adminFor' | transloco: { name: user.displayName }
                    "
                    (change)="update(user, { isPlatformAdmin: !user.isPlatformAdmin })"
                  />
                </td>
                <td>
                  <input
                    type="checkbox"
                    class="table__checkbox"
                    [checked]="user.isActive"
                    [disabled]="saving() || user.id === me()?.id"
                    [attr.aria-label]="
                      'admin.users.activeFor' | transloco: { name: user.displayName }
                    "
                    (change)="update(user, { isActive: !user.isActive })"
                  />
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
      @case ('error') {
        <p class="card" role="alert">{{ 'errors.unknown' | transloco }}</p>
      }
      @default {
        <p aria-busy="true">{{ 'common.loading' | transloco }}</p>
      }
    }
  `,
})
export class UsersPage implements OnInit {
  private readonly store = inject(Store);
  protected readonly users = this.store.selectSignal(adminFeature.selectUsers);
  protected readonly status = this.store.selectSignal(adminFeature.selectUsersStatus);
  protected readonly saving = this.store.selectSignal(adminFeature.selectSaving);
  protected readonly me = this.store.selectSignal(authFeature.selectUser);

  ngOnInit(): void {
    this.store.dispatch(adminPageActions.usersOpened());
  }

  protected update(user: User, request: { isActive?: boolean; isPlatformAdmin?: boolean }): void {
    this.store.dispatch(
      adminPageActions.userUpdateSubmitted({
        userId: user.id,
        request: {
          isActive: request.isActive ?? null,
          isPlatformAdmin: request.isPlatformAdmin ?? null,
        },
      }),
    );
  }
}
