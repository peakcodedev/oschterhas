import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-landing-page',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="page">
      <h1>{{ 'app.name' | transloco }}</h1>
      <p>{{ 'landing.intro' | transloco }}</p>
    </main>
  `,
})
export class LandingPage {}
