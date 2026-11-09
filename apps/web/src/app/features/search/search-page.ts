import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

/** Public search view of an instance. Map and hares follow in M1. */
@Component({
  selector: 'app-search-page',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="page">
      <h1>{{ 'app.name' | transloco }}</h1>
      <p>{{ 'search.comingSoon' | transloco }}</p>
    </main>
  `,
})
export class SearchPage {}
