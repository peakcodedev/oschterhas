/** UI languages: German, Swiss German (Mundart), English. */
export const LANGUAGES = ['de', 'gsw', 'en'] as const;
export type Language = (typeof LANGUAGES)[number];
export const DEFAULT_LANGUAGE: Language = 'de';
