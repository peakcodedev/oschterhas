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
      gap: 12px;
      margin-bottom: 16px;
      padding: 10px 14px;
      border-radius: 10px;
      background: var(--color-success-bg);
    }
    .banner--error {
      background: var(--color-error-bg);
      color: var(--color-error);
    }
    .banner__close {
      min-width: 44px;
      min-height: 44px;
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
