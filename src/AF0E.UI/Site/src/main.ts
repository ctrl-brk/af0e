import {bootstrapApplication} from '@angular/platform-browser';
import {appConfig} from './app/app.config';
import {AppComponent} from './app/components/app.component';
import mapboxgl from 'mapbox-gl';
// Serve the CSP worker as a static asset to avoid bundler-specific import shape/runtime differences.
mapboxgl.workerUrl = new URL('mapbox-gl-csp-worker.js', document.baseURI).toString();

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error());
