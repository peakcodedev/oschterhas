import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  signal,
  viewChild,
} from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { AdminHare, GpxWaypoint } from '../../../core/api/api-types';
import { adminPageActions } from '../state/admin.actions';
import { AdminHareMap, HareMove } from './admin-hare-map';

/** Map and list of the hares: select, move, rename, add, delete, reset "found". */
@Component({
  selector: 'app-hares-section',
  imports: [TranslocoPipe, AdminHareMap],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './hares-section.html',
  styleUrl: './hares-section.scss',
})
export class HaresSection {
  readonly instanceId = input.required<string>();
  readonly hares = input.required<AdminHare[]>();
  readonly selected = input<AdminHare | null>(null);
  readonly preview = input<GpxWaypoint[]>([]);
  readonly saving = input(false);

  private readonly store = inject(Store);
  private readonly transloco = inject(TranslocoService);
  private readonly map = viewChild(AdminHareMap);

  protected readonly onlyHidden = signal(false);
  protected readonly visibleHares = computed(() =>
    this.onlyHidden() ? this.hares().filter((h) => h.foundAt === null) : this.hares(),
  );
  protected readonly editName = linkedSignal(() => this.selected()?.name ?? '');
  protected readonly editDescription = linkedSignal(() => this.selected()?.description ?? '');

  protected displayName(hare: AdminHare): string {
    return hare.name ?? this.transloco.translate('hare.defaultName', { number: hare.number });
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected select(hareId: string | null): void {
    this.store.dispatch(adminPageActions.hareSelected({ hareId }));
  }

  protected moved(move: HareMove): void {
    const hare = this.hares().find((h) => h.id === move.hareId);
    if (hare) {
      this.save(hare.id, {
        name: hare.name,
        description: hare.description,
        lat: move.lat,
        lon: move.lon,
      });
    }
  }

  protected saveSelected(event: Event): void {
    event.preventDefault();
    const hare = this.selected();
    if (hare) {
      this.save(hare.id, {
        name: this.editName().trim() || null,
        description: this.editDescription().trim() || null,
        lat: hare.lat,
        lon: hare.lon,
      });
    }
  }

  protected add(): void {
    const center = this.map()?.center() ?? { lat: 46.8, lon: 8.23 };
    this.store.dispatch(
      adminPageActions.hareSaveSubmitted({
        instanceId: this.instanceId(),
        hareId: null,
        request: { name: null, description: null, lat: center.lat, lon: center.lon },
      }),
    );
  }

  protected remove(hare: AdminHare): void {
    if (
      confirm(
        this.transloco.translate('admin.hares.confirmDelete', { name: this.displayName(hare) }),
      )
    ) {
      this.store.dispatch(
        adminPageActions.hareDeleteConfirmed({ instanceId: this.instanceId(), hareId: hare.id }),
      );
    }
  }

  protected reset(hare: AdminHare): void {
    this.store.dispatch(
      adminPageActions.hareResetClicked({ instanceId: this.instanceId(), hareId: hare.id }),
    );
  }

  private save(
    hareId: string,
    request: { name: string | null; description: string | null; lat: number; lon: number },
  ): void {
    this.store.dispatch(
      adminPageActions.hareSaveSubmitted({ instanceId: this.instanceId(), hareId, request }),
    );
  }
}
