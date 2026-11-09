import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import * as L from 'leaflet';
import { PublicHare } from '../../core/api/api-types';
import { GeoPosition } from '../../features/search/state/search.models';
import { buildHarePopup, Translate } from './hare-popup';
import { createBaseLayers } from './map-layers';

const SWITZERLAND: L.LatLngTuple = [46.8, 8.23];

/** Leaflet map with one numbered marker per hare and the own position. */
@Component({
  selector: 'app-hare-map',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<div #map class="hare-map" role="application" [attr.aria-label]="ariaLabel()"></div>',
  styles: `
    :host {
      display: block;
      height: 100%;
    }
    .hare-map {
      height: 100%;
      width: 100%;
    }
  `,
})
export class HareMap {
  readonly hares = input.required<PublicHare[]>();
  readonly position = input<GeoPosition | null>(null);
  readonly ariaLabel = input('');

  private readonly element = viewChild.required<ElementRef<HTMLDivElement>>('map');
  private readonly transloco = inject(TranslocoService);
  private readonly translation = toSignal(this.transloco.selectTranslation());
  private readonly ready = signal(false);
  private readonly destroyRef = inject(DestroyRef);

  private map?: L.Map;
  private layersControl?: L.Control.Layers;
  private readonly hareLayer = L.layerGroup();
  private readonly positionLayer = L.layerGroup();
  private fitted = false;

  constructor() {
    afterNextRender(() => {
      const map = L.map(this.element().nativeElement, {
        center: SWITZERLAND,
        zoom: 8,
        zoomControl: true,
      });
      this.hareLayer.addTo(map);
      this.positionLayer.addTo(map);
      this.map = map;
      this.ready.set(true);
    });

    effect(() => {
      const translation = this.translation();
      if (this.ready() && translation && Object.keys(translation).length > 0) {
        this.renderHares(this.hares());
        this.ensureLayersControl();
      }
    });

    effect(() => {
      const position = this.position();
      if (this.ready()) {
        this.renderPosition(position);
      }
    });

    this.destroyRef.onDestroy(() => this.map?.remove());
  }

  /** Pans to the own position (keeps the zoom if already close). */
  centerOnPosition(position: GeoPosition): void {
    this.map?.setView([position.lat, position.lon], Math.max(this.map.getZoom(), 16));
  }

  private readonly t: Translate = (key, params) => this.transloco.translate(key, params);

  private ensureLayersControl(): void {
    if (!this.map || this.layersControl) {
      return;
    }
    const layers = createBaseLayers();
    layers[0].layer.addTo(this.map);
    this.layersControl = L.control
      .layers(Object.fromEntries(layers.map(({ key, layer }) => [this.t(key), layer])), undefined, {
        position: 'topright',
      })
      .addTo(this.map);
  }

  private renderHares(hares: PublicHare[]): void {
    this.hareLayer.clearLayers();
    for (const hare of hares) {
      L.marker([hare.lat, hare.lon], {
        icon: hareIcon(hare),
        keyboard: true,
        title: String(hare.number),
      })
        .bindPopup(() => buildHarePopup(hare, this.t))
        .addTo(this.hareLayer);
    }

    if (!this.fitted && hares.length > 0 && this.map) {
      this.map.invalidateSize();
      this.map.fitBounds(L.latLngBounds(hares.map((h) => [h.lat, h.lon] as L.LatLngTuple)), {
        padding: [32, 32],
        maxZoom: 17,
      });
      this.fitted = true;
    }
  }

  private renderPosition(position: GeoPosition | null): void {
    this.positionLayer.clearLayers();
    if (!position) {
      return;
    }
    const center: L.LatLngTuple = [position.lat, position.lon];
    L.circle(center, {
      radius: position.accuracy,
      className: 'own-position__accuracy',
      interactive: false,
    }).addTo(this.positionLayer);
    L.circleMarker(center, { radius: 8, className: 'own-position', interactive: false }).addTo(
      this.positionLayer,
    );
  }
}

function hareIcon(hare: PublicHare): L.DivIcon {
  const label = document.createElement('span');
  label.className = 'hare-marker__label';
  label.textContent = String(hare.number);
  return L.divIcon({
    className: hare.foundAt ? 'hare-marker hare-marker--found' : 'hare-marker',
    html: label,
    iconSize: [32, 32],
    iconAnchor: [16, 16],
    popupAnchor: [0, -16],
  });
}
