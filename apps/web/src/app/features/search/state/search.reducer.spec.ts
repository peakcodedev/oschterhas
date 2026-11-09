import { PublicHare, PublicInstance } from '../../../core/api/api-types';
import { geolocationActions, publicInstanceApiActions, searchPageActions } from './search.actions';
import { initialSearchState, searchFeature, SearchState } from './search.reducer';

const hare = (number: number, found = false): PublicHare => ({
  id: `h${number}`,
  number,
  name: null,
  description: null,
  lat: 47 + number / 1000,
  lon: 8,
  foundAt: found ? '2027-03-28T10:00:00Z' : null,
  foundBy: found ? 'Lina' : null,
});

const instance = (overrides: Partial<PublicInstance> = {}): PublicInstance => ({
  name: 'Ostersuche Tann',
  description: null,
  eventDate: '2027-03-28',
  status: 'Active',
  totalHares: 3,
  foundHares: 1,
  hares: [hare(3), hare(1, true), hare(2)],
  ...overrides,
});

const reduce = (...actions: Parameters<typeof searchFeature.reducer>[1][]): SearchState =>
  actions.reduce((state, action) => searchFeature.reducer(state, action), initialSearchState);

const select = (state: SearchState) => ({ search: state });

describe('search reducer', () => {
  it('is loading after the page opened', () => {
    expect(reduce(searchPageActions.opened()).loadStatus).toBe('loading');
  });

  it('stores instance and hares sorted by number', () => {
    const state = reduce(
      searchPageActions.opened(),
      publicInstanceApiActions.loadSucceeded({ instance: instance() }),
    );

    expect(state.loadStatus).toBe('loaded');
    expect(state.instance?.name).toBe('Ostersuche Tann');
    expect(searchFeature.selectHares(select(state)).map((h) => h.number)).toEqual([1, 2, 3]);
    expect(searchFeature.selectHaresVisible(select(state))).toBe(true);
  });

  it('counts progress from the hares', () => {
    const state = reduce(publicInstanceApiActions.loadSucceeded({ instance: instance() }));

    expect(searchFeature.selectProgress(select(state))).toEqual({ found: 1, total: 3 });
  });

  it('uses server counts and hides hares for a draft', () => {
    const state = reduce(
      publicInstanceApiActions.loadSucceeded({
        instance: instance({ status: 'Draft', hares: null, totalHares: 5, foundHares: 0 }),
      }),
    );

    expect(searchFeature.selectHaresVisible(select(state))).toBe(false);
    expect(searchFeature.selectHares(select(state))).toEqual([]);
    expect(searchFeature.selectProgress(select(state))).toEqual({ found: 0, total: 5 });
  });

  it.each(['notFound', 'error'] as const)('stores load failure %s', (reason) => {
    expect(reduce(publicInstanceApiActions.loadFailed({ reason })).loadStatus).toBe(reason);
  });

  it('tracks locating, position and errors', () => {
    const position = { lat: 47.27, lon: 8.85, accuracy: 12 };

    const locating = reduce(searchPageActions.locateMeClicked());
    const located = reduce(
      searchPageActions.locateMeClicked(),
      geolocationActions.positionChanged({ position }),
    );
    const failed = reduce(
      searchPageActions.locateMeClicked(),
      geolocationActions.failed({ reason: 'denied' }),
    );

    expect(locating.locating).toBe(true);
    expect(located).toMatchObject({ locating: false, position, positionError: null });
    expect(failed).toMatchObject({ locating: false, positionError: 'denied' });
  });

  it('resets when the page is closed', () => {
    const state = reduce(
      publicInstanceApiActions.loadSucceeded({ instance: instance() }),
      searchPageActions.closed(),
    );

    expect(state).toEqual(initialSearchState);
  });
});
