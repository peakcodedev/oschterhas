import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguagePicker } from '../../shared/language/language-picker';

@Component({
  selector: 'app-landing-page',
  imports: [TranslocoPipe, LanguagePicker, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: `
    .landing__title {
      margin: var(--space-8) 0 var(--space-6);
    }
    .landing__logo {
      display: block;
      width: min(240px, 70vw);
      height: auto;
    }
  `,
  template: `
    <main class="page">
      <app-language-picker />
      <h1 class="landing__title">
        <img
          class="landing__logo"
          src="brand/oschterhas-logo-hoch.svg"
          width="240"
          height="168"
          [alt]="'app.name' | transloco"
        />
      </h1>
      <p>{{ 'landing.intro' | transloco }}</p>
      <p>
        <a routerLink="/datenschutz">{{ 'privacy.title' | transloco }}</a>
      </p>
    </main>
  `,
})
export class LandingPage {}
