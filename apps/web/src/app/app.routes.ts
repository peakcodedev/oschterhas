import { Routes } from '@angular/router';
import { hostIs } from './core/host/host.guards';

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
    loadComponent: () => import('./features/search/search-page').then((m) => m.SearchPage),
  },
  { path: '**', redirectTo: '' },
];
