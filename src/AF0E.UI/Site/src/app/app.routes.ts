import { Routes } from '@angular/router';
import {LogbookComponent} from './components/logbook/logbook/logbook.component';
import {HomeComponent} from './components/home/home.component';

export const routes: Routes = [
  {
    path: '',
    component: HomeComponent
  },
  {
    path: 'logbook',
    component: LogbookComponent,
  },
  {
    path: 'logbook/:call',
    component: LogbookComponent,
  }
];
