import { ChangeDetectionStrategy, Component, input, output, linkedSignal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { CreateInstanceRequest, InstanceDetail } from '../../../core/api/api-types';

/** Create or edit an instance. The slug (address) can only be changed by platform admins. */
@Component({
  selector: 'app-instance-form',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instance-form.html',
})
export class InstanceForm {
  readonly instance = input<InstanceDetail | null>(null);
  readonly canEditSlug = input(false);
  readonly saving = input(false);
  readonly baseDomain = input.required<string>();
  readonly submitted = output<CreateInstanceRequest>();
  readonly cancelled = output<void>();

  protected readonly slug = linkedSignal(() => this.instance()?.slug ?? '');
  protected readonly name = linkedSignal(() => this.instance()?.name ?? '');
  protected readonly eventDate = linkedSignal(() => this.instance()?.eventDate ?? '');
  protected readonly description = linkedSignal(() => this.instance()?.description ?? '');

  protected value(event: Event): string {
    return (event.target as HTMLInputElement | HTMLTextAreaElement).value;
  }

  protected submit(event: Event): void {
    event.preventDefault();
    this.submitted.emit({
      slug: this.slug().trim().toLowerCase(),
      name: this.name().trim(),
      eventDate: this.eventDate() || null,
      description: this.description().trim() || null,
    });
  }
}
