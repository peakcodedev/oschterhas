import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

/** Administration area. Login and instance management follow in M4. */
@Component({
  selector: 'app-admin-page',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="page">
      <h1>{{ 'admin.title' | transloco }}</h1>
      <p>{{ 'admin.comingSoon' | transloco }}</p>
    </main>
  `,
})
export class AdminPage {}
