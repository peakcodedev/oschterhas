import { DOCUMENT, inject, Injectable } from '@angular/core';
import { isLanguage, Language } from '../../core/i18n/languages';

const KEY = 'oschterhas.language';

/** Remembers the chosen language in this browser (convenience only; failures are ignored). */
@Injectable({ providedIn: 'root' })
export class LanguageStorage {
  private readonly window = inject(DOCUMENT).defaultView;

  load(): Language | null {
    try {
      const value = this.window?.localStorage.getItem(KEY);
      return isLanguage(value) ? value : null;
    } catch {
      return null;
    }
  }

  save(language: Language): void {
    try {
      this.window?.localStorage.setItem(KEY, language);
    } catch {
      // Private mode or blocked storage: the choice simply is not remembered.
    }
  }

  browserLanguages(): readonly string[] {
    return this.window?.navigator.languages ?? [];
  }
}
