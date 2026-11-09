import { PublicHare } from '../../core/api/api-types';

export type Translate = (key: string, params?: Record<string, unknown>) => string;

/** Name of the hare, or "Hase {number}" in the current language when it has none. */
export function hareDisplayName(hare: PublicHare, t: Translate): string {
  return hare.name ?? t('hare.defaultName', { number: hare.number });
}

/**
 * Popup content as DOM nodes. Texts are set via textContent only (never innerHTML),
 * so names and descriptions from GPX files cannot inject markup.
 */
export function buildHarePopup(
  hare: PublicHare,
  t: Translate,
  doc: Document = document,
): HTMLElement {
  const root = doc.createElement('div');
  root.className = 'hare-popup';

  const title = doc.createElement('strong');
  title.className = 'hare-popup__title';
  title.textContent = hareDisplayName(hare, t);
  root.append(title);

  if (hare.description) {
    const description = doc.createElement('p');
    description.className = 'hare-popup__description';
    description.textContent = hare.description;
    root.append(description);
  }

  const status = doc.createElement('p');
  status.className = hare.foundAt
    ? 'hare-popup__status hare-popup__status--found'
    : 'hare-popup__status';
  status.textContent = hare.foundAt
    ? hare.foundBy
      ? t('hare.foundBy', { name: hare.foundBy })
      : t('hare.found')
    : t('hare.hidden');
  root.append(status);

  return root;
}
