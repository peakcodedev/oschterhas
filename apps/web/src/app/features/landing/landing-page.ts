import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguagePicker } from '../../shared/language/language-picker';

@Component({
  selector: 'app-landing-page',
  imports: [TranslocoPipe, LanguagePicker, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="page">
      <app-language-picker />
      <h1>{{ 'app.name' | transloco }}</h1>
      <p>{{ 'landing.intro' | transloco }}</p>
      <p>
        <a routerLink="/datenschutz">{{ 'privacy.title' | transloco }}</a>
      </p>
    </main>
  `,
})
export class LandingPage {}
