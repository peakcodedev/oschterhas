import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  linkedSignal,
  output,
  viewChild,
} from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { PublicHare } from '../../core/api/api-types';
import { hareDisplayName } from '../../shared/map/hare-popup';
import { FoundDialogState } from './state/search.models';

/** Asks for confirmation and an optional nickname before a hare is marked as found. */
@Component({
  selector: 'app-found-dialog',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './found-dialog.html',
  styleUrl: './found-dialog.scss',
})
export class FoundDialog {
  readonly hare = input<PublicHare | null>(null);
  readonly state = input.required<FoundDialogState>();
  readonly lastNickname = input<string | null>(null);
  readonly submitted = output<string | null>();
  readonly closed = output<void>();

  protected readonly maxNicknameLength = 40;
  protected readonly nickname = linkedSignal(() => this.lastNickname() ?? '');
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly transloco = inject(TranslocoService);

  constructor() {
    effect(() => {
      const dialog = this.dialog().nativeElement;
      const open = this.state().status !== 'closed' && this.hare() !== null;
      if (open && !dialog.open) {
        dialog.showModal();
      } else if (!open && dialog.open) {
        dialog.close();
      }
    });
  }

  protected title(hare: PublicHare): string {
    return this.transloco.translate('foundDialog.title', {
      name: hareDisplayName(hare, (key, params) => this.transloco.translate(key, params)),
    });
  }

  protected onInput(event: Event): void {
    this.nickname.set((event.target as HTMLInputElement).value);
  }

  protected submit(event: Event): void {
    event.preventDefault();
    if (this.state().status === 'submitting') {
      return;
    }
    const nickname = this.nickname().trim();
    this.submitted.emit(nickname.length > 0 ? nickname : null);
  }

  /** Escape key or cancel button. */
  protected cancel(event?: Event): void {
    event?.preventDefault();
    this.closed.emit();
  }
}
