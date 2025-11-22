import {Component, DestroyRef, ElementRef, inject, input, ViewChild, ViewEncapsulation} from '@angular/core';
import {ActivationQsoModel} from '../../../models/activation-qso.model';
import * as mapbox from 'mapbox-gl';
import {point} from '@turf/helpers';
import greatCircle from '@turf/great-circle';
import {environment} from '../../../../environments/environment';
import {PotaActivationModel} from '../../../models/pota-activation.model';

@Component({
  selector: 'app-activation-map',
  templateUrl: './activation-map.component.html',
  styleUrl: './activation-map.component.scss',
  encapsulation: ViewEncapsulation.None,
})
export class PotaActivationMapComponent {
  private _destroyRef = inject(DestroyRef);
  private _firstLoad = true;
  private _popupTimeout: any;
  protected map!: mapbox.Map;

  @ViewChild('map') mapElement?: ElementRef<HTMLDivElement>;

  logEntries = input.required<ActivationQsoModel[]>();
  activation = input.required<PotaActivationModel>();

  public onTabChange() {
    if (!this._firstLoad)
      return;

    this._firstLoad = false;
    this.setupMap(this.logEntries().filter(q => q.lat && q.long));
    setTimeout(() => this.map.resize());
  }

  private setupMap(contacts: ActivationQsoModel[]) {
    let hunters = contacts.filter(x => x.p2p.length === 0);
    let activators = contacts.filter(x => x.p2p.length !== 0);

    let bounds = contacts.reduce((bounds, q) =>
      bounds.extend([q.long!, q.lat!]),
      new mapbox.LngLatBounds([contacts[0].long!, contacts[0].lat!], [contacts[0].long!, contacts[0].lat!]));

    this.map = new mapbox.Map({
      accessToken: environment.mapBoxKey,
      container: this.mapElement!.nativeElement,
      style: 'mapbox://styles/mapbox/outdoors-v12',
      center: [this.activation().long!, this.activation().lat!],
      zoom: 7,
      projection: 'mercator'
    });

    this.map.on('load', () => {
      this.addContactsSource('hunters', hunters);
      this.addContactsSource('activators', activators);
      this.map.addSource('my-park', {
        'type': 'geojson',
        'data': {
          'type': 'Feature',
          'geometry': {
            'type': 'Point',
            'coordinates': [this.activation().long!, this.activation().lat!]
          },
          'properties': {}
        }
      });
      this.map.addSource('lines', {
        'type': 'geojson',
        'data': {
          'type': 'FeatureCollection',
          'features': contacts.map(q => {
            const from = point([this.activation().long!, this.activation().lat!]);
            const to = point([q.long!, q.lat!]);
            const options = {npoints: 25}; // Define the resolution of the line
            return greatCircle(from, to, options);
          })
        }
      });

      this.map.addLayer({
        'id': 'lines-layer',
        'source': 'lines',
        'type': 'line',
        'layout': {},
        'paint': {
          'line-color': 'darkviolet',
          'line-width': 0.5
        }
      });
      this.map.addLayer({
        'id': 'my-park-layer',
        'source': 'my-park',
        'type': 'circle',
        'paint': {
          'circle-radius': 5,
          'circle-color': 'red',
          'circle-stroke-width': 1,
          'circle-stroke-color': 'white'
        }
      });
      this.map.addLayer({
        'id': 'hunters-layer',
        'source': 'hunters',
        'type': 'circle',
        'paint': {
          'circle-radius': 5,
          'circle-color': 'yellow',
          'circle-stroke-width': 1,
          'circle-stroke-color': 'black'
        }
      });
      this.map.addLayer({
        'id': 'activators-layer',
        'source': 'activators',
        'type': 'circle',
        'paint': {
          'circle-radius': 5,
          'circle-color': 'green',
          'circle-stroke-width': 1,
          'circle-stroke-color': 'black'
        }
      });

      this.map.fitBounds(bounds, {padding: 20});
    });

    const popup = new mapbox.Popup({
      closeButton: false,
      closeOnClick: false
    });

    this.map.on('mouseenter', ['hunters-layer', 'activators-layer'], (e) => {
      clearTimeout(this._popupTimeout);
      this.map.getCanvas().style.cursor = 'default';
      //@ts-ignore
      const coordinates = e.features![0].geometry.coordinates.slice();
      const qso: any = JSON.parse(e.features![0].properties!['qso']);

      popup.setLngLat(coordinates)
        .setHTML(`<a href='/logbook/${qso.call}' target='_blank'>${qso.call}</a> ${qso.band} ${qso.mode} ${qso.p2p}`) //qso.p2p array somehow gets split by comma
        .addTo(this.map);

      // Setup mouseleave for popup
      popup.getElement()!.addEventListener('mouseenter', () => clearTimeout(this._popupTimeout));
      popup.getElement()!.addEventListener('mouseleave', () => {
        this._popupTimeout = setTimeout(() => popup.remove(), 50); // short delay before hiding popup
      });
    });

    this.map.on('mouseleave', ['hunters-layer', 'activators-layer'], () => {
      this.map.getCanvas().style.cursor = '';
      this._popupTimeout = setTimeout(() => popup.remove(), 50); // Allows for quick mouse re-entry
    });

    this.map.addControl(new mapbox.NavigationControl());
    this._destroyRef.onDestroy(() => this.map.remove());
  }

  private addContactsSource(id: string, contacts: ActivationQsoModel[]) {
    this.map.addSource(id, {
      'type': 'geojson',
      'data': {
        'type': 'FeatureCollection',
        'features': contacts.map(q => ({
          'type': 'Feature',
          'geometry': {
            'type': 'Point',
            'coordinates': [q.long!, q.lat!]
          },
          'properties': {qso: q}
        }))
      }
    });
  }
}
