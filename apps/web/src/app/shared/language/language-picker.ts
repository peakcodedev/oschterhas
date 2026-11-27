import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { isLanguage, LANGUAGE_BADGES, LANGUAGES } from '../../core/i18n/languages';
import { uiActions } from '../../store/ui/ui.actions';
import { uiFeature } from '../../store/ui/ui.reducer';

/** Compact language switcher: flag and short code (DE, CH, EN) on top of a native select. */
@Component({
  selector: 'app-language-picker',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="language-picker">
      <span class="visually-hidden">{{ 'language.label' | transloco }}</span>
      <span class="language-picker__current" aria-hidden="true">
        <img class="language-picker__flag" [src]="badge().flag" alt="" width="20" height="20" />
        <span>{{ badge().code }}</span>
        <span class="language-picker__chevron">▾</span>
      </span>
      <!-- The native select lies invisibly on top: accessible, touch-friendly, full names in the list. -->
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
    .language-picker {
      position: relative;
      display: inline-flex;
      flex: none;
    }
    .language-picker__current {
      display: inline-flex;
      align-items: center;
      gap: var(--space-1);
      min-height: 48px;
      padding: 0 var(--space-2) 0 var(--space-2);
      border: 1px solid var(--color-border-strong);
      border-radius: var(--radius-pill);
      background: var(--color-surface);
      color: var(--color-text);
      font-family: var(--label-font);
      font-size: var(--label-size);
      font-weight: 700;
    }
    .language-picker__flag {
      width: 20px;
      height: 20px;
      border-radius: 50%;
      object-fit: cover;
      box-shadow: 0 0 0 1px var(--color-border);
    }
    .language-picker__chevron {
      font-size: var(--klein-size);
      color: var(--color-muted);
    }
    .language-picker__select {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
      opacity: 0;
      font-size: 16px; /* below 16 px iOS zooms in on focus */
      cursor: pointer;
    }
    .language-picker:focus-within .language-picker__current {
      outline: 3px solid var(--color-position);
      outline-offset: 2px;
    }
  `,
})
export class LanguagePicker {
  private readonly store = inject(Store);
  protected readonly languages = LANGUAGES;
  protected readonly language = this.store.selectSignal(uiFeature.selectLanguage);
  protected readonly badge = computed(() => LANGUAGE_BADGES[this.language()]);

  protected choose(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (isLanguage(value)) {
      this.store.dispatch(uiActions.languageChosen({ language: value }));
    }
  }
}
