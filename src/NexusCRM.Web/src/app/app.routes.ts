import { Routes } from '@angular/router';
import { ShellComponent } from './shell/shell.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./auth/pages/login.page').then((m) => m.LoginPage),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'reports' },
      {
        path: 'search',
        loadComponent: () => import('./search/pages/search.page').then((m) => m.SearchPage),
      },
      {
        path: 'reports',
        loadComponent: () => import('./reports/pages/reports.page').then((m) => m.ReportsPage),
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('./customers/pages/customer-list.page').then((m) => m.CustomerListPage),
      },
      {
        path: 'customers/:id',
        loadComponent: () =>
          import('./customers/pages/customer-detail.page').then((m) => m.CustomerDetailPage),
      },
      {
        path: 'leads',
        loadComponent: () => import('./leads/pages/lead-board.page').then((m) => m.LeadBoardPage),
      },
      {
        path: 'deals',
        loadComponent: () => import('./deals/pages/deal-board.page').then((m) => m.DealBoardPage),
      },
      {
        path: 'workflows',
        loadComponent: () => import('./workflows/pages/workflows.page').then((m) => m.WorkflowsPage),
      },
      {
        path: 'plugins',
        loadComponent: () => import('./plugins/pages/plugins.page').then((m) => m.PluginsPage),
      },
    ],
  },
];

