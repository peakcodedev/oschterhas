import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { Store } from '@ngrx/store';
import { map } from 'rxjs';
import { authActions } from '../../core/auth/auth.actions';
import { authFeature } from '../../core/auth/auth.reducer';
import { MessageBanner } from './components/message-banner';

const LOGIN_REASONS = new Set(['no_access', 'disabled', 'failed']);

/** Frame of the administration: login screen for anonymous visitors, navigation and pages for signed-in users. */
@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslocoPipe, MessageBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-shell.html',
  styleUrl: './admin-shell.scss',
})
export class AdminShell implements OnInit {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);

  protected readonly status = this.store.selectSignal(authFeature.selectStatus);
  protected readonly user = this.store.selectSignal(authFeature.selectUser);
  private readonly loginParam = toSignal(this.route.queryParamMap.pipe(map((p) => p.get('login'))));
  protected readonly loginReason = computed(() => {
    const reason = this.loginParam();
    return reason && LOGIN_REASONS.has(reason) ? reason : null;
  });

  ngOnInit(): void {
    this.store.dispatch(authActions.checkRequested());
  }

  protected login(): void {
    const path = window.location.pathname;
    this.store.dispatch(
      authActions.loginClicked({ returnUrl: path === '/' ? '/instanzen' : path }),
    );
  }

  protected logout(): void {
    this.store.dispatch(authActions.logoutClicked());
  }

  protected retry(): void {
    this.store.dispatch(authActions.checkRequested());
  }
}
