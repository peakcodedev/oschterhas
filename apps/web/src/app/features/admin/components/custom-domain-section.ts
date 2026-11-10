import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, linkedSignal } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { InstanceDetail } from '../../../core/api/api-types';
import { environment } from '../../../../environments/environment';
import { adminPageActions } from '../state/admin.actions';
import { uiFeature } from '../../../store/ui/ui.reducer';

/**
 * Own domain of a search (e.g. ostern.ceviduernten.ch): platform admins set, check and remove it;
 * organizers see the status only.
 */
@Component({
  selector: 'app-custom-domain-section',
  imports: [TranslocoPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './custom-domain-section.html',
})
export class CustomDomainSection {
  readonly instance = input.required<InstanceDetail>();
  readonly canEdit = input(false);
  readonly saving = input(false);

  private readonly store = inject(Store);
  private readonly transloco = inject(TranslocoService);
  protected readonly dnsTarget = environment.customDomainTarget;
  protected readonly locale = this.store.selectSignal(uiFeature.selectLocale);
  protected readonly domain = linkedSignal(() => this.instance().customDomain ?? '');

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected save(event: Event): void {
    event.preventDefault();
    this.store.dispatch(
      adminPageActions.customDomainSubmitted({
        instanceId: this.instance().id,
        domain: this.domain().trim(),
      }),
    );
  }

  protected verify(): void {
    this.store.dispatch(
      adminPageActions.customDomainVerifyClicked({ instanceId: this.instance().id }),
    );
  }

  protected remove(): void {
    const domain = this.instance().customDomain ?? '';
    if (confirm(this.transloco.translate('admin.customDomain.confirmRemove', { domain }))) {
      this.store.dispatch(
        adminPageActions.customDomainRemoveConfirmed({ instanceId: this.instance().id }),
      );
    }
  }
}
