import { DETAIL_ZOOM, zoomLevel } from './zoom-level';

describe('zoomLevel', () => {
  it('shows dots below the detail zoom and numbers from it on', () => {
    expect(zoomLevel(14)).toBe('far');
    expect(zoomLevel(DETAIL_ZOOM - 1)).toBe('far');
    expect(zoomLevel(DETAIL_ZOOM)).toBe('near');
    expect(zoomLevel(19)).toBe('near');
  });
});
