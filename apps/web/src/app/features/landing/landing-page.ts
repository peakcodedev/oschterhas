import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguagePicker } from '../../shared/language/language-picker';

@Component({
  selector: 'app-landing-page',
  imports: [TranslocoPipe, LanguagePicker],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="page">
      <app-language-picker />
      <h1>{{ 'app.name' | transloco }}</h1>
      <p>{{ 'landing.intro' | transloco }}</p>
    </main>
  `,
})
export class LandingPage {}
