import { ChangeDetectionStrategy, Component, DOCUMENT, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { TranslocoPipe } from '@jsverse/transloco';
import { filter, map, of } from 'rxjs';

/** Offers a reload when the service worker has downloaded a new app version. */
@Component({
  selector: 'app-update-banner',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (updateReady()) {
      <div class="update-banner" role="status">
        <span>{{ 'update.ready' | transloco }}</span>
        <button type="button" class="button button--small" (click)="reload()">
          {{ 'update.reload' | transloco }}
        </button>
      </div>
    }
  `,
  styles: `
    .update-banner {
      position: fixed;
      left: 16px;
      right: 16px;
      bottom: calc(16px + env(safe-area-inset-bottom));
      z-index: 2000;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-2);
      max-width: 32rem;
      margin: 0 auto;
      padding: var(--space-2) var(--space-2) var(--space-2) var(--space-4);
      border-radius: var(--radius-md);
      background: var(--ink);
      color: var(--weiss);
      box-shadow: 0 4px 16px rgb(0 0 0 / 30%);
    }
  `,
})
export class UpdateBanner {
  private readonly updates = inject(SwUpdate);
  private readonly document = inject(DOCUMENT);

  protected readonly updateReady = toSignal(
    this.updates.isEnabled
      ? this.updates.versionUpdates.pipe(
          filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'),
          map(() => true),
        )
      : of(false),
    { initialValue: false },
  );

  protected reload(): void {
    this.document.location.reload();
  }
}
