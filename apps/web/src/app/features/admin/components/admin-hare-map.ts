import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import * as L from 'leaflet';
import { AdminHare, GpxWaypoint } from '../../../core/api/api-types';
import { createBaseLayers } from '../../../shared/map/map-layers';

export interface HareMove {
  hareId: string;
  lat: number;
  lon: number;
}

/**
 * Map for organizers: hares can be dragged to a new position and selected for editing.
 * A GPX preview is shown as dashed markers on top.
 */
@Component({
  selector: 'app-admin-hare-map',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<div #map class="admin-map" role="application" [attr.aria-label]="ariaLabel()"></div>',
  styles: `
    :host {
      display: block;
    }
    .admin-map {
      height: min(60vh, 480px);
      border-radius: 12px;
    }
  `,
})
export class AdminHareMap {
  readonly hares = input.required<AdminHare[]>();
  readonly selectedHareId = input<string | null>(null);
  readonly preview = input<GpxWaypoint[]>([]);
  readonly ariaLabel = input('');
  readonly selected = output<string>();
  readonly moved = output<HareMove>();

  private readonly element = viewChild.required<ElementRef<HTMLDivElement>>('map');
  private readonly transloco = inject(TranslocoService);
  private readonly translation = toSignal(this.transloco.selectTranslation());
  private readonly destroyRef = inject(DestroyRef);
  private readonly ready = signal(false);
  private map?: L.Map;
  private readonly hareLayer = L.layerGroup();
  private readonly previewLayer = L.layerGroup();
  private fittedTo = '';

  constructor() {
    afterNextRender(() => {
      const element = this.element().nativeElement;
      const map = L.map(element, { center: [46.8, 8.23], zoom: 8 });
      this.hareLayer.addTo(map);
      this.previewLayer.addTo(map);
      this.map = map;
      const resizeObserver = new ResizeObserver(() => map.invalidateSize());
      resizeObserver.observe(element);
      this.destroyRef.onDestroy(() => resizeObserver.disconnect());
      this.ready.set(true);
    });

    effect(() => {
      const translation = this.translation();
      if (!this.ready() || !translation || Object.keys(translation).length === 0 || !this.map) {
        return;
      }
      this.ensureLayers();
      this.renderHares(this.hares(), this.selectedHareId());
      this.renderPreview(this.preview());
    });

    this.destroyRef.onDestroy(() => this.map?.remove());
  }

  /** Map center, e.g. for a new hare. */
  center(): { lat: number; lon: number } {
    const c = this.map?.getCenter() ?? L.latLng(46.8, 8.23);
    return { lat: c.lat, lon: c.lng };
  }

  private layersAdded = false;

  private ensureLayers(): void {
    if (this.layersAdded || !this.map) {
      return;
    }
    const layers = createBaseLayers();
    layers[0].layer.addTo(this.map);
    L.control
      .layers(
        Object.fromEntries(layers.map(({ key, layer }) => [this.transloco.translate(key), layer])),
      )
      .addTo(this.map);
    this.layersAdded = true;
  }

  private renderHares(hares: AdminHare[], selectedId: string | null): void {
    this.hareLayer.clearLayers();
    for (const hare of hares) {
      const label = document.createElement('span');
      label.textContent = String(hare.number);
      const marker = L.marker([hare.lat, hare.lon], {
        draggable: true,
        keyboard: true,
        title: hare.name ?? this.transloco.translate('hare.defaultName', { number: hare.number }),
        icon: L.divIcon({
          className: [
            'hare-marker',
            hare.foundAt ? 'hare-marker--found' : '',
            hare.id === selectedId ? 'hare-marker--selected' : '',
          ].join(' '),
          html: label,
          iconSize: [32, 32],
          iconAnchor: [16, 16],
        }),
      });
      marker.on('click', () => this.selected.emit(hare.id));
      marker.on('dragend', () => {
        const position = marker.getLatLng();
        this.moved.emit({ hareId: hare.id, lat: position.lat, lon: position.lng });
      });
      marker.addTo(this.hareLayer);
    }
    this.fit(
      hares.map((h) => [h.lat, h.lon] as L.LatLngTuple),
      `hares:${hares.length > 0}`,
    );
  }

  private renderPreview(waypoints: GpxWaypoint[]): void {
    this.previewLayer.clearLayers();
    for (const waypoint of waypoints) {
      L.circleMarker([waypoint.lat, waypoint.lon], {
        radius: 9,
        className: 'preview-marker',
        interactive: false,
      }).addTo(this.previewLayer);
    }
    if (waypoints.length > 0) {
      this.fit(
        waypoints.map((w) => [w.lat, w.lon] as L.LatLngTuple),
        `preview:${waypoints.length}:${waypoints[0].lat}`,
      );
    }
  }

  /** Fits the view once per data set, so dragging or saving does not jump the map around. */
  private fit(points: L.LatLngTuple[], key: string): void {
    if (!this.map || points.length === 0 || this.fittedTo === key) {
      return;
    }
    this.fittedTo = key;
    this.map.invalidateSize();
    this.map.fitBounds(L.latLngBounds(points), { padding: [32, 32], maxZoom: 17 });
  }
}
