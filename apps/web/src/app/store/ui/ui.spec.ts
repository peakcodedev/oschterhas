import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoService } from '@jsverse/transloco';
import { provideMockActions } from '@ngrx/effects/testing';
import { ROOT_EFFECTS_INIT } from '@ngrx/effects';
import { Action } from '@ngrx/store';
import { firstValueFrom, Observable, Subject } from 'rxjs';
import { uiActions } from './ui.actions';
import { applyLanguage, detectLanguageOnStart, rememberLanguage } from './ui.effects';
import { LanguageStorage } from './language-storage';
import { initialUiState, uiFeature } from './ui.reducer';

describe('ui reducer', () => {
  it('stores the language and maps it to a locale', () => {
    const state = uiFeature.reducer(initialUiState, uiActions.languageChosen({ language: 'gsw' }));

    expect(state.language).toBe('gsw');
    expect(uiFeature.selectLocale({ ui: state })).toBe('gsw');
    expect(uiFeature.selectLocale({ ui: initialUiState })).toBe('de-CH');
  });
});

describe('ui effects', () => {
  function setup(storage: Partial<LanguageStorage>) {
    const actions$ = new Subject<Action>();
    const setActiveLang = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideMockActions(() => actions$ as Observable<Action>),
        { provide: LanguageStorage, useValue: storage },
        { provide: TranslocoService, useValue: { setActiveLang } },
      ],
    });
    return { actions$, setActiveLang };
  }

  it('prefers the stored language', async () => {
    const { actions$ } = setup({ load: () => 'en', browserLanguages: () => ['de-CH'] });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => detectLanguageOnStart()));

    actions$.next({ type: ROOT_EFFECTS_INIT });

    expect(await result).toEqual(uiActions.languageDetected({ language: 'en' }));
  });

  it('falls back to the browser language', async () => {
    const { actions$ } = setup({ load: () => null, browserLanguages: () => ['gsw', 'de'] });
    const result = firstValueFrom(TestBed.runInInjectionContext(() => detectLanguageOnStart()));

    actions$.next({ type: ROOT_EFFECTS_INIT });

    expect(await result).toEqual(uiActions.languageDetected({ language: 'gsw' }));
  });

  it('applies the language to Transloco and the document', () => {
    const { actions$, setActiveLang } = setup({});
    const subscription = TestBed.runInInjectionContext(() => applyLanguage()).subscribe();

    actions$.next(uiActions.languageChosen({ language: 'en' }));

    expect(setActiveLang).toHaveBeenCalledWith('en');
    expect(TestBed.inject(DOCUMENT).documentElement.lang).toBe('en');
    subscription.unsubscribe();
  });

  it('remembers only an explicit choice', () => {
    const save = vi.fn();
    const { actions$ } = setup({ save });
    const subscription = TestBed.runInInjectionContext(() => rememberLanguage()).subscribe();

    actions$.next(uiActions.languageDetected({ language: 'de' }));
    actions$.next(uiActions.languageChosen({ language: 'gsw' }));

    expect(save).toHaveBeenCalledExactlyOnceWith('gsw');
    subscription.unsubscribe();
  });
});
