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

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideAnimationsAsync(),
    provideRouter(routes, withComponentInputBinding()),
    provideAuth0({
      domain: 'dev-4l6joodw0kczibgl.us.auth0.com',
      clientId: 'sNpxyLre7xkR55bd6kHXURvJSGLkzaRX',
      authorizationParams: {
        redirect_uri: window.location.origin,
        audience: 'https://af0e.logbook.api'
      },
      httpInterceptor: {
        allowedList: [
          {
            uri: '/api/*',
            // uriMatcher: (uri) => {
            //   console.log('🔍 Checking URI:', uri);
            //   return uri.includes('/api/');
            // },
            tokenOptions: {
              authorizationParams: {
                audience: 'https://af0e.logbook.api'
              }
            }
          }
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
