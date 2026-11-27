import * as L from 'leaflet';

const swisstopoAttribution =
  '© <a href="https://www.swisstopo.admin.ch/" target="_blank" rel="noopener">swisstopo</a>';

/**
 * Tiles bypass the Angular service worker: the browser loads them directly (checked against img-src).
 * Through the service worker they would be fetch() requests under the policy the worker was installed
 * with; ngsw-worker.js does not change between builds, so an installed worker keeps an old policy and
 * answers every blocked tile with "504 Gateway Timeout". Tiles are not cached offline anyway.
 */
const SERVICE_WORKER_BYPASS = '?ngsw-bypass=true';

export const TILE_URLS = {
  swisstopo: `https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.pixelkarte-farbe/default/current/3857/{z}/{x}/{y}.jpeg${SERVICE_WORKER_BYPASS}`,
  aerial: `https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.swissimage/default/current/3857/{z}/{x}/{y}.jpeg${SERVICE_WORKER_BYPASS}`,
  osm: `https://tile.openstreetmap.org/{z}/{x}/{y}.png${SERVICE_WORKER_BYPASS}`,
} as const;

/** Base maps. swisstopo WMTS (Web Mercator) is the default; OpenStreetMap as fallback outside Switzerland. */
export function createBaseLayers(): { key: string; layer: L.TileLayer }[] {
  return [
    {
      key: 'map.layer.swisstopo',
      layer: L.tileLayer(TILE_URLS.swisstopo, {
        maxNativeZoom: 18,
        maxZoom: 20,
        attribution: swisstopoAttribution,
      }),
    },
    {
      key: 'map.layer.aerial',
      layer: L.tileLayer(TILE_URLS.aerial, {
        maxNativeZoom: 19,
        maxZoom: 20,
        attribution: swisstopoAttribution,
      }),
    },
    {
      key: 'map.layer.osm',
      layer: L.tileLayer(TILE_URLS.osm, {
        maxNativeZoom: 19,
        maxZoom: 20,
        attribution:
          '© <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>',
      }),
    },
  ];
}
