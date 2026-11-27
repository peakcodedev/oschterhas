import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { adminPageActions } from '../state/admin.actions';
import { adminFeature } from '../state/admin.reducer';

/** Success or error message of the last administration action. */
@Component({
  selector: 'app-message-banner',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (message(); as message) {
      <div
        class="banner"
        [class.banner--error]="message.kind === 'error'"
        [attr.role]="message.kind === 'error' ? 'alert' : 'status'"
      >
        <span>{{ message.key | transloco }}</span>
        <button
          type="button"
          class="banner__close"
          (click)="dismiss()"
          [attr.aria-label]="'common.close' | transloco"
        >
          ×
        </button>
      </div>
    }
  `,
  styles: `
    .banner {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-2);
      margin-bottom: var(--space-4);
      padding: var(--space-1) var(--space-1) var(--space-1) var(--space-4);
      border-radius: var(--radius-md);
      background: var(--color-success);
      color: var(--color-success-text);
      font-weight: 700;
    }
    .banner--error {
      background: var(--color-error);
      color: var(--color-error-text);
    }
    .banner__close {
      min-width: 48px;
      min-height: 48px;
      border: 0;
      background: transparent;
      font-size: 1.4rem;
      color: inherit;
      cursor: pointer;
    }
  `,
})
export class MessageBanner {
  private readonly store = inject(Store);
  protected readonly message = this.store.selectSignal(adminFeature.selectMessage);

  protected dismiss(): void {
    this.store.dispatch(adminPageActions.messageDismissed());
  }
}
