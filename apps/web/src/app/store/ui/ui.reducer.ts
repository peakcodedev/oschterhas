import { createFeature, createReducer, createSelector, on } from '@ngrx/store';
import { DEFAULT_LANGUAGE, Language, LOCALES } from '../../core/i18n/languages';
import { uiActions } from './ui.actions';

export interface UiState {
  language: Language;
}

export const initialUiState: UiState = { language: DEFAULT_LANGUAGE };

export const uiFeature = createFeature({
  name: 'ui',
  reducer: createReducer(
    initialUiState,
    on(uiActions.languageDetected, uiActions.languageChosen, (state, { language }): UiState => ({
      ...state,
      language,
    })),
  ),
  extraSelectors: ({ selectLanguage }) => ({
    /** Angular locale for DatePipe & co. */
    selectLocale: createSelector(selectLanguage, (language) => LOCALES[language]),
  }),
});
