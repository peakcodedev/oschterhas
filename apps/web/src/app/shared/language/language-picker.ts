import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { isLanguage, LANGUAGES } from '../../core/i18n/languages';
import { uiActions } from '../../store/ui/ui.actions';
import { uiFeature } from '../../store/ui/ui.reducer';

/** Compact language switcher (native select: accessible and touch-friendly). */
@Component({
  selector: 'app-language-picker',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="language-picker">
      <span class="visually-hidden">{{ 'language.label' | transloco }}</span>
      <select class="language-picker__select" [value]="language()" (change)="choose($event)">
        @for (lang of languages; track lang) {
          <option [value]="lang" [selected]="lang === language()">
            {{ 'language.names.' + lang | transloco }}
          </option>
        }
      </select>
    </label>
  `,
  styles: `
    .language-picker__select {
      min-height: 48px;
      padding: 0 var(--space-2);
      border: 1px solid var(--color-border-strong);
      border-radius: var(--radius-sm);
      background: var(--color-surface);
      font: inherit;
      color: var(--color-text);
    }
    .language-picker__select:focus-visible {
      outline: 3px solid var(--color-position);
      outline-offset: 1px;
    }
  `,
})
export class LanguagePicker {
  private readonly store = inject(Store);
  protected readonly languages = LANGUAGES;
  protected readonly language = this.store.selectSignal(uiFeature.selectLanguage);

  protected choose(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (isLanguage(value)) {
      this.store.dispatch(uiActions.languageChosen({ language: value }));
    }
  }
}
