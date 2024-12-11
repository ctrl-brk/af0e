import {ApplicationConfig, provideZoneChangeDetection} from '@angular/core';
import {provideAnimationsAsync} from '@angular/platform-browser/animations/async';
import {provideRouter, withComponentInputBinding} from '@angular/router';

import {routes} from './app.routes';
import {provideHttpClient} from '@angular/common/http';
import {MessageService} from 'primeng/api';
import {LogService} from './shared/log.service';
import {providePrimeNG} from 'primeng/config';
import Aura from '@primeng/themes/aura';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: {
          darkModeSelector: 'light',
          cssLayer: false
        }
      },
      ripple: true
    }),
    MessageService,
    LogService
  ]
};
