import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  OnDestroy,
  OnInit,
  viewChild,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { HareMap } from '../../shared/map/hare-map';
import { FoundDialog } from './found-dialog';
import { searchPageActions } from './state/search.actions';
import { searchFeature } from './state/search.reducer';

/** Public search view of an instance: map with all hares, progress and own position. */
@Component({
  selector: 'app-search-page',
  imports: [TranslocoPipe, HareMap, FoundDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './search-page.html',
  styleUrl: './search-page.scss',
})
export class SearchPage implements OnInit, OnDestroy {
  private readonly store = inject(Store);
  private readonly map = viewChild(HareMap);
  private centerOnNextPosition = false;

  protected readonly loadStatus = this.store.selectSignal(searchFeature.selectLoadStatus);
  protected readonly instance = this.store.selectSignal(searchFeature.selectInstance);
  protected readonly hares = this.store.selectSignal(searchFeature.selectHares);
  protected readonly haresVisible = this.store.selectSignal(searchFeature.selectHaresVisible);
  protected readonly progress = this.store.selectSignal(searchFeature.selectProgress);
  protected readonly position = this.store.selectSignal(searchFeature.selectPosition);
  protected readonly positionError = this.store.selectSignal(searchFeature.selectPositionError);
  protected readonly locating = this.store.selectSignal(searchFeature.selectLocating);
  protected readonly canMarkFound = this.store.selectSignal(searchFeature.selectCanMarkFound);
  protected readonly foundDialog = this.store.selectSignal(searchFeature.selectFoundDialog);
  protected readonly dialogHare = this.store.selectSignal(searchFeature.selectDialogHare);
  protected readonly lastNickname = this.store.selectSignal(searchFeature.selectNickname);
  protected readonly live = this.store.selectSignal(searchFeature.selectLive);

  constructor() {
    // Center the map once on the first position after "locate me" was clicked.
    effect(() => {
      const position = this.position();
      if (position && this.centerOnNextPosition) {
        this.centerOnNextPosition = false;
        this.map()?.centerOnPosition(position);
      }
    });
  }

  ngOnInit(): void {
    this.store.dispatch(searchPageActions.opened());
  }

  ngOnDestroy(): void {
    this.store.dispatch(searchPageActions.closed());
  }

  protected retry(): void {
    this.store.dispatch(searchPageActions.retryClicked());
  }

  protected openFoundDialog(hareId: string): void {
    this.store.dispatch(searchPageActions.foundDialogOpened({ hareId }));
  }

  protected submitFound(nickname: string | null): void {
    const hareId = this.foundDialog().hareId;
    if (hareId) {
      this.store.dispatch(searchPageActions.markFoundSubmitted({ hareId, nickname }));
    }
  }

  protected closeFoundDialog(): void {
    this.store.dispatch(searchPageActions.foundDialogClosed());
  }

  protected locateMe(): void {
    const position = this.position();
    if (position) {
      this.map()?.centerOnPosition(position);
      return;
    }
    this.centerOnNextPosition = true;
    this.store.dispatch(searchPageActions.locateMeClicked());
  }
}
