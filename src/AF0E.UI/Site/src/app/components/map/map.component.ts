import {Component, DestroyRef, inject, OnInit, ViewEncapsulation} from '@angular/core';
import * as mapbox from 'mapbox-gl';
import {environment} from "../../../environments/environment";

@Component({
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss',
  encapsulation: ViewEncapsulation.None,
})
export class MapComponent implements OnInit {
  private _destroyRef = inject(DestroyRef);
  protected map!: mapbox.Map;

  ngOnInit(): void {
    this.setupMap();
  }

  private setupMap() {
    this.map = new mapbox.Map({
      accessToken: environment.mapBoxKey,
      container: 'parks-map',
      style: 'mapbox://styles/mapbox/outdoors-v12',
      center: [-105.7821, 39.5501],
      zoom: 7,
      projection: 'mercator'
    });

    this.map.on('load', ()=> {
      this.map?.addSource('activated-parks', {
        type: 'geojson',
        data: './api/v1/logbook/pota/geojson/activations/all'
      });

      this.map?.addSource('not-activated-parks-nearby', {
        type: 'geojson',
        data: './api/v1/logbook/pota/geojson/parks/not-activated/CO,WY'
      });

      this.map?.addLayer({
        'id': 'activated-parks-layer',
        'type': 'circle',
        'source': 'activated-parks',
        'paint': {
          'circle-radius': 5,
          'circle-stroke-width': 1,
          'circle-color': 'red',
          'circle-stroke-color': 'white'
        }
      });

      this.map?.addLayer({
        'id': 'na-parks-layer',
        'type': 'circle',
        'source': 'not-activated-parks-nearby',
        'paint': {
          'circle-radius': 5,
          'circle-stroke-width': 1,
          'circle-color': 'yellow',
          'circle-stroke-color': 'black'
        }
      });
    });

    this.map.on('mouseenter', 'activated-parks-layer', () => {
      this.map.getCanvas().style.cursor = 'pointer';
    });

    this.map.on('mouseleave', 'activated-parks-layer', () => {
      this.map.getCanvas().style.cursor = '';
    });

    this.map.on('mouseenter', 'na-parks-layer', () => {
      this.map.getCanvas().style.cursor = 'pointer';
    });

    this.map.on('mouseleave', 'na-parks-layer', () => {
      this.map.getCanvas().style.cursor = '';
    });

    this.map.on('click', 'activated-parks-layer', (e) => {
      // @ts-ignore
      const coordinates = e.features[0].geometry.coordinates.slice();
      let html = '';
      let i;
      for (i = 0; ; i++) {
        // @ts-ignore
        let p = e.features[0].properties[i];
        if (!p) break;
        p = JSON.parse(p);
        html += `<li><a href='/pota/activations/${p.activationId}' target='_blank' title='View log'>${p.startDate.substring(2, 10)}</a> <a href='https://pota.app/#/park/${p.parkNum}' target="_blank" title='${p.parkName}'>${p.parkNum}</a></li>`;
      }
      html = `<h4>${i} activation${i > 1 ? 's' : ''}</h4><ul class='map-popup-list'>${html}</ul>`;

      this.showPopup(coordinates, e, html);
});

    this.map.on('click', 'na-parks-layer', (e) => {
      // @ts-ignore
      const coordinates = e.features[0].geometry.coordinates.slice();
      // @ts-ignore
      const p = e.features[0].properties;
      // @ts-ignore
      let html = `<div class='map-popup-na'><a href='https://pota.app/#/park/${p.parkNum}' target="_blank">${p.parkNum}</a><br>${p.parkName}</div>`;

      this.showPopup(coordinates, e, html);
    });

    this.map.addControl(new mapbox.NavigationControl());
    this._destroyRef.onDestroy(() => this.map.remove());
  }

  private showPopup(coordinates: any, e: mapbox.MapMouseEvent, html: string) {
    // Ensure that if the map is zoomed out such that multiple copies of the feature are visible, the popup appears over the copy being pointed to.
    if (['mercator', 'equirectangular'].includes(this.map.getProjection().name)) {
      while (Math.abs(e.lngLat.lng - coordinates[0]) > 180) {
        coordinates[0] += e.lngLat.lng > coordinates[0] ? 360 : -360;
      }
    }

    new mapbox.Popup()
      .setLngLat(coordinates)
      .setHTML(html)
      .addTo(this.map);
  }
}
