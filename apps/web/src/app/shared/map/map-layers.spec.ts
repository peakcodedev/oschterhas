import { createBaseLayers, TILE_URLS } from './map-layers';

describe('map layers', () => {
  it('lets every tile request bypass the service worker', () => {
    for (const url of Object.values(TILE_URLS)) {
      expect(url).toMatch(/\?ngsw-bypass=true$/);
    }
  });

  it('builds all base layers from these URLs', () => {
    // Leaflet keeps the URL template in a private field.
    const templates = createBaseLayers().map(
      ({ layer }) => (layer as unknown as { _url: string })._url,
    );
    expect(templates).toEqual([TILE_URLS.swisstopo, TILE_URLS.aerial, TILE_URLS.osm]);
  });
});
