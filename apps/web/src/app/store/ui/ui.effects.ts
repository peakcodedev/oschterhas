// Only effects may be exported from this file: app.config.ts passes the whole module to provideEffects().
import { DOCUMENT, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { Actions, createEffect, ofType, ROOT_EFFECTS_INIT } from '@ngrx/effects';
import { map, tap } from 'rxjs';
import { detectLanguage } from '../../core/i18n/languages';
import { uiActions } from './ui.actions';
import { LanguageStorage } from './language-storage';

export const detectLanguageOnStart = createEffect(
  (actions$ = inject(Actions), storage = inject(LanguageStorage)) =>
    actions$.pipe(
      ofType(ROOT_EFFECTS_INIT),
      map(() =>
        uiActions.languageDetected({
          language: storage.load() ?? detectLanguage(storage.browserLanguages()),
        }),
      ),
    ),
  { functional: true },
);

/** Switches Transloco and the document language (screen readers, hyphenation). */
export const applyLanguage = createEffect(
  (actions$ = inject(Actions), transloco = inject(TranslocoService), document = inject(DOCUMENT)) =>
    actions$.pipe(
      ofType(uiActions.languageDetected, uiActions.languageChosen),
      tap(({ language }) => {
        transloco.setActiveLang(language);
        document.documentElement.lang = language;
      }),
    ),
  { functional: true, dispatch: false },
);

export const rememberLanguage = createEffect(
  (actions$ = inject(Actions), storage = inject(LanguageStorage)) =>
    actions$.pipe(
      ofType(uiActions.languageChosen),
      tap(({ language }) => storage.save(language)),
    ),
  { functional: true, dispatch: false },
);
