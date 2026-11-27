import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import QRCode from 'qrcode';

/** QR code of the public address, printable as a small poster (print view shows only this). */
@Component({
  selector: 'app-qr-poster',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure class="qr-poster">
      @if (dataUrl(); as src) {
        <img
          class="qr-poster__code"
          [src]="src"
          [alt]="'admin.qr.alt' | transloco: { url: url() }"
          width="256"
          height="256"
        />
      }
      <figcaption>
        <strong class="qr-poster__title">{{ title() }}</strong>
        <span class="qr-poster__url">{{ url() }}</span>
      </figcaption>
    </figure>
  `,
  styles: `
    .qr-poster {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 16px;
      margin: 0;
    }
    .qr-poster__code {
      width: 160px;
      height: 160px;
      image-rendering: pixelated;
    }
    .qr-poster__title,
    .qr-poster__url {
      display: block;
    }
    .qr-poster__url {
      word-break: break-all;
      color: var(--color-muted);
    }
  `,
})
export class QrPoster {
  readonly url = input.required<string>();
  readonly title = input('');
  protected readonly dataUrl = signal<string | null>(null);

  constructor() {
    effect(() => {
      const url = this.url();
      QRCode.toDataURL(url, { margin: 1, width: 512, errorCorrectionLevel: 'M' })
        .then((data) => this.dataUrl.set(data))
        .catch(() => this.dataUrl.set(null));
    });
  }
}
