import * as L from 'leaflet';

const swisstopoAttribution =
  '© <a href="https://www.swisstopo.admin.ch/" target="_blank" rel="noopener">swisstopo</a>';

/** Base maps. swisstopo WMTS (Web Mercator) is the default; OpenStreetMap as fallback outside Switzerland. */
export function createBaseLayers(): { key: string; layer: L.TileLayer }[] {
  return [
    {
      key: 'map.layer.swisstopo',
      layer: L.tileLayer(
        'https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.pixelkarte-farbe/default/current/3857/{z}/{x}/{y}.jpeg',
        { maxNativeZoom: 18, maxZoom: 20, attribution: swisstopoAttribution },
      ),
    },
    {
      key: 'map.layer.aerial',
      layer: L.tileLayer(
        'https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.swissimage/default/current/3857/{z}/{x}/{y}.jpeg',
        { maxNativeZoom: 19, maxZoom: 20, attribution: swisstopoAttribution },
      ),
    },
    {
      key: 'map.layer.osm',
      layer: L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxNativeZoom: 19,
        maxZoom: 20,
        attribution:
          '© <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>',
      }),
    },
  ];
}
