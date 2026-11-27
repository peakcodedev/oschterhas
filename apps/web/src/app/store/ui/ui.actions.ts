import { createActionGroup, props } from '@ngrx/store';
import { Language } from '../../core/i18n/languages';

export const uiActions = createActionGroup({
  source: 'UI',
  events: {
    /** At startup: stored choice or browser language. */
    languageDetected: props<{ language: Language }>(),
    languageChosen: props<{ language: Language }>(),
  },
});
