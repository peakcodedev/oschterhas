import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { InstanceDetail, Organizer } from '../../../core/api/api-types';
import { adminPageActions } from '../state/admin.actions';

/** Platform admins invite and remove the organizers of an instance. */
@Component({
  selector: 'app-organizers-section',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="card">
      <h2>{{ 'admin.organizers.title' | transloco }}</h2>
      @if (instance().organizers.length === 0) {
        <p>{{ 'admin.organizers.empty' | transloco }}</p>
      }
      <ul class="list">
        @for (organizer of instance().organizers; track organizer.userId) {
          <li class="list__row">
            <span>
              <strong>{{ organizer.displayName }}</strong>
              <span class="list__meta">{{ organizer.email }}</span>
              @if (organizer.invitationPending) {
                <span class="tag">{{ 'admin.organizers.pending' | transloco }}</span>
              }
            </span>
            <span class="list__actions">
              @if (organizer.invitationPending) {
                <button
                  type="button"
                  class="button button--secondary button--small"
                  [disabled]="saving()"
                  (click)="resend(organizer)"
                >
                  {{ 'admin.organizers.resend' | transloco }}
                </button>
              }
              <button
                type="button"
                class="button button--danger button--small"
                [disabled]="saving()"
                (click)="remove(organizer)"
              >
                {{ 'admin.organizers.remove' | transloco }}
              </button>
            </span>
          </li>
        }
      </ul>

      <form class="form form--inline" (submit)="add($event)">
        <label class="form__field">
          <span class="form__label">{{ 'admin.organizers.email' | transloco }}</span>
          <input
            class="input"
            type="email"
            name="email"
            required
            maxlength="254"
            autocomplete="off"
            [value]="email()"
            (input)="email.set(value($event))"
          />
        </label>
        <label class="form__field">
          <span class="form__label">{{ 'admin.organizers.name' | transloco }}</span>
          <input
            class="input"
            name="displayName"
            required
            maxlength="200"
            autocomplete="off"
            [value]="name()"
            (input)="name.set(value($event))"
          />
        </label>
        <button type="submit" class="button" [disabled]="saving()">
          {{ 'admin.organizers.invite' | transloco }}
        </button>
      </form>
      <p class="form__hint">{{ 'admin.organizers.inviteHint' | transloco }}</p>
    </section>
  `,
})
export class OrganizersSection {
  readonly instance = input.required<InstanceDetail>();
  readonly saving = input(false);

  private readonly store = inject(Store);
  private readonly transloco = inject(TranslocoService);
  protected readonly email = signal('');
  protected readonly name = signal('');

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected add(event: Event): void {
    event.preventDefault();
    this.store.dispatch(
      adminPageActions.addOrganizerSubmitted({
        instanceId: this.instance().id,
        request: { email: this.email().trim(), displayName: this.name().trim() },
      }),
    );
    this.email.set('');
    this.name.set('');
  }

  protected remove(organizer: Organizer): void {
    if (
      confirm(
        this.transloco.translate('admin.organizers.confirmRemove', { name: organizer.displayName }),
      )
    ) {
      this.store.dispatch(
        adminPageActions.removeOrganizerConfirmed({
          instanceId: this.instance().id,
          userId: organizer.userId,
        }),
      );
    }
  }

  protected resend(organizer: Organizer): void {
    this.store.dispatch(
      adminPageActions.resendInvitationClicked({
        instanceId: this.instance().id,
        userId: organizer.userId,
      }),
    );
  }
}
