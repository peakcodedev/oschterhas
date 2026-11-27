import { PublicHare, PublicInstance } from '../../../core/api/api-types';
import {
  geolocationActions,
  instanceEventsActions,
  publicInstanceApiActions,
  searchPageActions,
} from './search.actions';
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

  describe('found dialog', () => {
    const loaded = publicInstanceApiActions.loadSucceeded({ instance: instance() });
    const found = { hareId: 'h2', foundAt: '2027-03-28T11:00:00Z', foundBy: 'Urs' };

    it('opens for a hare and selects it', () => {
      const state = reduce(loaded, searchPageActions.foundDialogOpened({ hareId: 'h2' }));

      expect(state.foundDialog).toEqual({ hareId: 'h2', status: 'open', error: null });
      expect(searchFeature.selectDialogHare(select(state))?.number).toBe(2);
    });

    it('remembers the nickname while submitting', () => {
      const state = reduce(
        loaded,
        searchPageActions.foundDialogOpened({ hareId: 'h2' }),
        searchPageActions.markFoundSubmitted({ hareId: 'h2', nickname: 'Urs' }),
      );

      expect(state.foundDialog.status).toBe('submitting');
      expect(state.nickname).toBe('Urs');
    });

    it('marks the hare and closes after success', () => {
      const state = reduce(
        loaded,
        searchPageActions.markFoundSubmitted({ hareId: 'h2', nickname: 'Urs' }),
        publicInstanceApiActions.markFoundSucceeded({ found }),
      );

      expect(state.foundDialog.status).toBe('closed');
      expect(state.entities['h2']).toMatchObject({ foundAt: found.foundAt, foundBy: 'Urs' });
      expect(searchFeature.selectProgress(select(state))).toEqual({ found: 2, total: 3 });
    });

    it('shows the failure reason', () => {
      const state = reduce(
        loaded,
        searchPageActions.markFoundSubmitted({ hareId: 'h2', nickname: null }),
        publicInstanceApiActions.markFoundFailed({ hareId: 'h2', reason: 'alreadyFound' }),
      );

      expect(state.foundDialog).toEqual({ hareId: 'h2', status: 'failed', error: 'alreadyFound' });
    });

    it('closes on cancel', () => {
      const state = reduce(
        loaded,
        searchPageActions.foundDialogOpened({ hareId: 'h2' }),
        searchPageActions.foundDialogClosed(),
      );

      expect(state.foundDialog.status).toBe('closed');
    });
  });

  describe('live updates', () => {
    const loaded = publicInstanceApiActions.loadSucceeded({ instance: instance() });

    it('applies found and reset events', () => {
      const found = reduce(
        loaded,
        instanceEventsActions.hareFound({
          found: { hareId: 'h3', foundAt: '2027-03-28T12:00:00Z', foundBy: null },
        }),
      );
      const reset = reduce(loaded, instanceEventsActions.hareReset({ hareId: 'h1' }));

      expect(found.entities['h3']?.foundAt).toBe('2027-03-28T12:00:00Z');
      expect(reset.entities['h1']).toMatchObject({ foundAt: null, foundBy: null });
    });

    it('ignores events for unknown hares', () => {
      const state = reduce(loaded, instanceEventsActions.hareReset({ hareId: 'unknown' }));

      expect(state.ids).toEqual(['h1', 'h2', 'h3']);
    });

    it('tracks the connection', () => {
      expect(reduce().live).toBe('connecting');
      expect(reduce(instanceEventsActions.connected()).live).toBe('connected');
      expect(reduce(instanceEventsActions.disconnected()).live).toBe('disconnected');
    });

    it('allows marking only while the search is active', () => {
      const active = reduce(loaded);
      const ended = reduce(
        publicInstanceApiActions.loadSucceeded({ instance: instance({ status: 'Ended' }) }),
      );

      expect(searchFeature.selectCanMarkFound(select(active))).toBe(true);
      expect(searchFeature.selectCanMarkFound(select(ended))).toBe(false);
    });
  });
});
