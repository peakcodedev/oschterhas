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
      padding: 2px 10px;
      border-radius: 999px;
      font-size: 0.85rem;
      font-weight: 600;
      background: var(--color-info-bg);
    }
    .badge--active {
      background: var(--color-success-bg);
    }
    .badge--ended {
      background: var(--color-border);
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<InstanceStatus>();
}
