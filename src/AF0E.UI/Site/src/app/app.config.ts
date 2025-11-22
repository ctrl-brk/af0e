import {ApplicationConfig, provideZoneChangeDetection} from '@angular/core';
import {provideRouter, withComponentInputBinding} from '@angular/router';
import {routes} from './routing/app.routes';
import {provideHttpClient, withInterceptors} from '@angular/common/http';
import {MessageService} from 'primeng/api';
import {LogService} from './shared/log.service';
import {providePrimeNG} from 'primeng/config';
import Aura from '@primeng/themes/aura';
import {provideAnimationsAsync} from '@angular/platform-browser/animations/async';
import {authHttpInterceptorFn, provideAuth0} from '@auth0/auth0-angular';
import {environment} from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideAnimationsAsync(),
    provideRouter(routes, withComponentInputBinding()),
    provideAuth0({
      domain: environment.auth0domain,
      clientId: environment.auth0clientId,
      authorizationParams: {
        redirect_uri: window.location.origin,
        audience: environment.auth0audience
      },
      httpInterceptor: {
        allowedList: [
          {
            uri: '/api/v1/logbook/qso*',
            httpMethod: 'GET',
            allowAnonymous: true,
          },
          {
            // QSO create operation - require authentication
            uri: '/api/v1/logbook/qso*',
            httpMethod: 'POST',
          },
          {
            // QSO update operation - require authentication
            uri: '/api/v1/logbook/qso*',
            httpMethod: 'PUT',
          },
          // {
          //   // POTA unconfirmed log - requires authentication
          //   uri: '/api/v1/pota/log/unconfirmed*',
          //   tokenOptions: {
          //     authorizationParams: {
          //       audience: 'https://af0e.logbook.api'
          //     }
          //   }
          // }
        ]
      }
    }),
    provideHttpClient(withInterceptors([authHttpInterceptorFn])),
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
