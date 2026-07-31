import {bootstrapApplication} from '@angular/platform-browser';
import {appConfig} from './app/app.config';
import {AppComponent} from './app/components/app.component';
import mapboxgl from 'mapbox-gl';
// Mapbox's CSP worker build is not a native ESM module.
// Vite's ?worker loader wraps it as a Worker constructor, which Mapbox expects on workerClass.
import MapboxWorker from 'mapbox-gl/dist/mapbox-gl-csp-worker.js?worker';

mapboxgl.workerClass = MapboxWorker;

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error());
