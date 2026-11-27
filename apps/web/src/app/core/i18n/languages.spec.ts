import { detectLanguage, isLanguage } from './languages';

describe('detectLanguage', () => {
  it.each([
    [['de-CH', 'de', 'en'], 'de'],
    [['gsw', 'de-CH'], 'gsw'],
    [['gsw-CH'], 'gsw'],
    [['en-GB', 'de'], 'en'],
    [['fr-CH', 'it', 'en-US'], 'en'],
    [['fr-CH', 'it'], 'de'],
    [[], 'de'],
  ])('%j → %s', (languages, expected) => {
    expect(detectLanguage(languages)).toBe(expected);
  });
});

describe('isLanguage', () => {
  it('accepts only offered languages', () => {
    expect(isLanguage('gsw')).toBe(true);
    expect(isLanguage('fr')).toBe(false);
    expect(isLanguage(null)).toBe(false);
  });
});
