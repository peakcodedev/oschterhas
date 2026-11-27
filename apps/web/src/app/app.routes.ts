import { Routes } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';
import * as authEffects from './core/auth/auth.effects';
import { authFeature } from './core/auth/auth.reducer';
import { hostIs } from './core/host/host.guards';
import * as adminEffects from './features/admin/state/admin.effects';
import { adminFeature } from './features/admin/state/admin.reducer';
import * as searchEffects from './features/search/state/search.effects';
import { searchFeature } from './features/search/state/search.reducer';

export const routes: Routes = [
  {
    // Privacy notice on every host (search, administration, landing page).
    path: 'datenschutz',
    loadComponent: () => import('./features/privacy/privacy-page').then((m) => m.PrivacyPage),
  },
  {
    path: '',
    canMatch: [hostIs('admin')],
    providers: [
      provideState(authFeature),
      provideState(adminFeature),
      provideEffects(authEffects, adminEffects),
    ],
    loadComponent: () => import('./features/admin/admin-shell').then((m) => m.AdminShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'instanzen' },
      // Target of the invitation link (registered redirect URI at Peakcode Identity).
      { path: 'login', redirectTo: 'instanzen' },
      {
        path: 'instanzen',
        loadComponent: () =>
          import('./features/admin/pages/instance-list-page').then((m) => m.InstanceListPage),
      },
      {
        path: 'instanzen/:instanceId',
        loadComponent: () =>
          import('./features/admin/pages/instance-detail-page').then((m) => m.InstanceDetailPage),
      },
      {
        path: 'benutzer',
        loadComponent: () => import('./features/admin/pages/users-page').then((m) => m.UsersPage),
      },
    ],
  },
  {
    path: '',
    canMatch: [hostIs('landing')],
    loadComponent: () => import('./features/landing/landing-page').then((m) => m.LandingPage),
  },
  {
    path: '',
    canMatch: [hostIs('instance')],
    providers: [provideState(searchFeature), provideEffects(searchEffects)],
    loadComponent: () => import('./features/search/search-page').then((m) => m.SearchPage),
  },
  { path: '**', redirectTo: '' },
];
