import {Routes} from '@angular/router';
import {HomeComponent} from '../components/home/home.component';
import {AboutComponent} from '../components/about/about.component';
import {StatsComponent} from '../components/stats/stats.component';
import {Status401Component} from '../components/error/401.component';
import {rolePermissionGuard} from './route-guards';
import {Roles} from '../shared/roles';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'logbook',
    children: [
      {
        path: '',
        loadComponent: () => import('../components/logbook/logbook.component').then(m => m.LogbookComponent)
      },
      {
        path: ':prefix/:call/:suffix',
        loadComponent: () => import('../components/logbook/logbook.component').then(m => m.LogbookComponent)
      },
      {
        path: ':prefix/:call',
        loadComponent: () => import('../components/logbook/logbook.component').then(m => m.LogbookComponent)
      },
      {
        path: ':call',
        loadComponent: () => import('../components/logbook/logbook.component').then(m => m.LogbookComponent)
      },
    ]
  },
  {
    path: 'db',
    children: [
      {
        path: 'qso',
        loadComponent: () => import('../components/logbook/log-entry.component').then(m => m.LogEntryComponent),
        canActivate: [rolePermissionGuard([Roles.Admin])],
      },
    ]
  },
  {
    path: 'pota',
    children: [
      {
        path: 'activations',
        loadComponent: () => import('../components/pota/activations/activations.component').then(m => m.PotaActivationsComponent),
      },
      {
        path: 'activations/:id',
        loadComponent: () => import('../components/pota/activation/activation.component').then(m => m.PotaActivationComponent),
      },
      {
        path: 'park/:parkNum/stats',
        loadComponent: () => import('../components/pota/park/stats/hunting.component').then(m => m.PotaParkHuntingComponent),
      },
      {
        path: 'log/unconfirmed',
        loadComponent: () => import('../components/pota/log/unconfirmed.component').then(m => m.PotaUnconfirmedComponent),
      },
    ]
  },
  {
    path: 'map',
    children: [
      {
        path: '',
        loadComponent: () => import('../components/map/map.component').then(m => m.MapComponent),
      },
    ]
  },
  {
    path: 'stats',
    component: StatsComponent,
  },
  {
    path: 'about',
    component: AboutComponent,
  },
  {
    path: 'unauthorized',
    component: Status401Component,
  },
];
