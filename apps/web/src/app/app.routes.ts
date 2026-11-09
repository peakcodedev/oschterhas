import { Routes } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';
import { hostIs } from './core/host/host.guards';
import * as searchEffects from './features/search/state/search.effects';
import { searchFeature } from './features/search/state/search.reducer';

export const routes: Routes = [
  {
    path: '',
    canMatch: [hostIs('admin')],
    loadComponent: () => import('./features/admin/admin-page').then((m) => m.AdminPage),
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
