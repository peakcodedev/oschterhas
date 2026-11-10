import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { LanguagePicker } from '../../shared/language/language-picker';

/** What Oschterhas stores and what it does not (revDSG). Available on every host at /datenschutz. */
@Component({
  selector: 'app-privacy-page',
  imports: [RouterLink, TranslocoPipe, LanguagePicker],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="page notice privacy">
      <div class="page-header">
        <a routerLink="/">← {{ 'privacy.back' | transloco }}</a>
        <app-language-picker />
      </div>
      <h1>{{ 'privacy.title' | transloco }}</h1>
      <p>{{ 'privacy.intro' | transloco }}</p>

      <h2>{{ 'privacy.searchers.title' | transloco }}</h2>
      <ul>
        <li>{{ 'privacy.searchers.noAccount' | transloco }}</li>
        <li>{{ 'privacy.searchers.found' | transloco }}</li>
        <li>{{ 'privacy.searchers.noIp' | transloco }}</li>
        <li>{{ 'privacy.searchers.location' | transloco }}</li>
        <li>{{ 'privacy.searchers.tiles' | transloco }}</li>
        <li>{{ 'privacy.searchers.storage' | transloco }}</li>
      </ul>

      <h2>{{ 'privacy.organizers.title' | transloco }}</h2>
      <ul>
        <li>{{ 'privacy.organizers.login' | transloco }}</li>
        <li>{{ 'privacy.organizers.data' | transloco }}</li>
      </ul>

      <h2>{{ 'privacy.hosting.title' | transloco }}</h2>
      <p>{{ 'privacy.hosting.text' | transloco }}</p>
    </main>
  `,
  styles: `
    .privacy li {
      margin-bottom: 8px;
    }
  `,
})
export class PrivacyPage {}
