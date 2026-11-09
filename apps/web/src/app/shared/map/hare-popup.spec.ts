import { PublicHare } from '../../core/api/api-types';
import { buildHarePopup, hareDisplayName, Translate } from './hare-popup';

const t: Translate = (key, params) => (params ? `${key}:${JSON.stringify(params)}` : key);

const hare = (overrides: Partial<PublicHare> = {}): PublicHare => ({
  id: 'h1',
  number: 7,
  name: 'Brunnen',
  description: null,
  lat: 47.27,
  lon: 8.85,
  foundAt: null,
  foundBy: null,
  ...overrides,
});

describe('hareDisplayName', () => {
  it('uses the own name', () => {
    expect(hareDisplayName(hare(), t)).toBe('Brunnen');
  });

  it('falls back to the translated default name with number', () => {
    expect(hareDisplayName(hare({ name: null }), t)).toBe('hare.defaultName:{"number":7}');
  });
});

describe('buildHarePopup', () => {
  it('shows name, description and hidden status', () => {
    const popup = buildHarePopup(hare({ description: 'Hauptstrasse 64' }), t);

    expect(popup.querySelector('.hare-popup__title')?.textContent).toBe('Brunnen');
    expect(popup.querySelector('.hare-popup__description')?.textContent).toBe('Hauptstrasse 64');
    expect(popup.querySelector('.hare-popup__status')?.textContent).toBe('hare.hidden');
  });

  it('shows who found the hare', () => {
    const popup = buildHarePopup(hare({ foundAt: '2027-03-28T10:00:00Z', foundBy: 'Lina' }), t);

    expect(popup.querySelector('.hare-popup__status--found')?.textContent).toBe(
      'hare.foundBy:{"name":"Lina"}',
    );
  });

  it('never interprets texts as HTML', () => {
    const popup = buildHarePopup(
      hare({ name: '<img src=x onerror=alert(1)>', description: '<b>fett</b>' }),
      t,
    );

    expect(popup.querySelector('img')).toBeNull();
    expect(popup.querySelector('b')).toBeNull();
    expect(popup.querySelector('.hare-popup__title')?.textContent).toBe(
      '<img src=x onerror=alert(1)>',
    );
  });
});
