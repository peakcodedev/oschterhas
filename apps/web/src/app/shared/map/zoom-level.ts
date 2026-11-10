import type * as L from 'leaflet';

/** From this zoom on, hares are shown as numbered markers; below as small dots. */
export const DETAIL_ZOOM = 16;

export type ZoomLevel = 'far' | 'near';

export function zoomLevel(zoom: number): ZoomLevel {
  return zoom < DETAIL_ZOOM ? 'far' : 'near';
}

/**
 * Keeps `data-zoom-level` on the map container in sync, so CSS can shrink dense markers to dots
 * at overview zoom (the 32 px tap area stays).
 */
export function trackZoomLevel(map: L.Map): void {
  const container = map.getContainer();
  const update = () => (container.dataset['zoomLevel'] = zoomLevel(map.getZoom()));
  map.on('zoomend', update);
  update();
}
