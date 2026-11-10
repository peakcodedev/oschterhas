import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { InstanceStatus } from '../../../core/api/api-types';

@Component({
  selector: 'app-status-badge',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class]="'badge badge--' + status().toLowerCase()">{{
    'admin.status.' + status() | transloco
  }}</span>`,
  styles: `
    .badge {
      display: inline-block;
      padding: 2px var(--space-2);
      border-radius: var(--radius-pill);
      font-size: var(--klein-size);
      line-height: var(--klein-line);
      font-weight: 700;
      background: var(--color-highlight);
      color: var(--ink);
    }
    .badge--active {
      background: var(--color-success);
      color: var(--color-success-text);
    }
    .badge--ended {
      background: var(--color-border);
      color: var(--ink);
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<InstanceStatus>();
}
