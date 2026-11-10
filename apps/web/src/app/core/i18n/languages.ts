/** UI languages: German, Swiss German (Mundart), English. */
export const LANGUAGES = ['de', 'gsw', 'en'] as const;
export type Language = (typeof LANGUAGES)[number];
export const DEFAULT_LANGUAGE: Language = 'de';

/** Angular locale for dates and numbers per UI language (registered in main.ts). */
export const LOCALES: Record<Language, string> = { de: 'de-CH', gsw: 'gsw', en: 'en-CH' };

export function isLanguage(value: unknown): value is Language {
  return typeof value === 'string' && (LANGUAGES as readonly string[]).includes(value);
}

/**
 * First language from the browser's list that we offer. `de-CH` (the usual Swiss setting) gets
 * standard German; Mundart only if the browser asks for `gsw` explicitly or the user chooses it.
 */
export function detectLanguage(browserLanguages: readonly string[]): Language {
  for (const tag of browserLanguages) {
    const primary = tag.toLowerCase().split('-')[0];
    if (isLanguage(primary)) {
      return primary;
    }
  }
  return DEFAULT_LANGUAGE;
}
