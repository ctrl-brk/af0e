import {Routes} from '@angular/router';
import {HomeComponent} from './components/home/home.component';
import {AboutComponent} from './components/about/about.component';
import {StatsComponent} from './components/stats/stats.component';

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
        loadComponent: () => import('./components/logbook/logbook.component').then(m => m.LogbookComponent)
      },
      {
        path: ':call',
        loadComponent: () => import('./components/logbook/logbook.component').then(m => m.LogbookComponent)
      },
    ]
  },
  {
    path: 'pota',
    children: [
      {
        path: 'activations',
        loadComponent: () => import('./components/pota/activations/activations.component').then(m => m.PotaActivationsComponent),
      },
      {
        path: 'activations/:id',
        loadComponent: () => import('./components/pota/activation/activation.component').then(m => m.PotaActivationComponent),
      }
    ]
  },
  {
    path: 'map',
    children: [
      {
        path: '',
        loadComponent: () => import('./components/map/map.component').then(m => m.MapComponent),
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
];
