import {Component, DestroyRef, inject, OnInit, ViewEncapsulation} from '@angular/core';
import {FormsModule} from '@angular/forms';
import * as mapbox from 'mapbox-gl';
import {environment} from "../../../environments/environment";
import {Fieldset} from 'primeng/fieldset';
import {ColorPicker} from 'primeng/colorpicker';
import {debounceTime, fromEvent, Subject, Subscription} from 'rxjs';
import {Utils} from '../../shared/utils';
import {PotaService} from '../../services/pota.service';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {Checkbox} from 'primeng/checkbox';
import {AutoComplete} from 'primeng/autocomplete';
import {PotaParkModel} from '../../models/pota-park.model';
import {BreakpointObserver} from '@angular/cdk/layout';

@Component({
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    Fieldset,
    ColorPicker,
    FormsModule,
    Checkbox,
    AutoComplete,
  ]
})
export class MapComponent implements OnInit {
  private _destroyRef = inject(DestroyRef);
  private _responsive = inject(BreakpointObserver);
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _colorChanges$ = new Subject<void>();
  private _popup: mapbox.Popup | undefined;
  private _state = {
    mapStyle: 'outdoors',
    notActivatedParks: {
      swLong: 0,
      swLat: 0,
      neLong: 0,
      neLat: 0,
      hydrated: false,
      data: undefined
    },
    activatedParks: {hydrated: false, data: undefined},
    activations: {hydrated: false, data: undefined},
  };
  private _notActivatedParksSub: Subscription | undefined;
  protected map!: mapbox.Map;
  protected pointColors = {
    first: '#ff0000',
    second: '#ff00ff',
    third: '#ffff00',
    fourth: '#1E90ff',
    default: '#f4a460',
  };
  protected showActivations = true;
  protected searchName:string | undefined;
  protected parksFound: PotaParkModel[] = [];
  isLessThan1000px = false;

  ngOnInit(): void {
    this.setupMap();
    this.setResponsive();
    const sub = this._colorChanges$.pipe(debounceTime(500)).subscribe(() => this.changePointColor());
    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private setResponsive(): void {
    const sub = this._responsive.observe('(max-width: 1000px)')
      .subscribe(x => this.isLessThan1000px = x.matches);
    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private setupMap() {
    this.map = new mapbox.Map({
      accessToken: environment.mapBoxKey,
      container: 'parks-map',
      style: `mapbox://styles/mapbox/${this._state.mapStyle}-v12`,
      center: [-105.7821, 39.5501],
      zoom: 7,
      //projection: 'mercator'
    });

    this.map.on('load', () => {
      this.map.fitBounds([[-109.05, 37.0], [-102.05, 41.0]], {padding: 10}); //CO bounds - SW, NE
    });

    // this gets called even on first map load
    this.map.on('style.load', () => {
      this.setupMapSourcesAndLayers();
      this.hydrateAndUpdateLayers(true);
    });

    this.map.on('mouseenter', ['activations-layer', 'activated-parks-layer', 'na-parks-dynamic-layer'], () => {
      this.map.getCanvas().style.cursor = 'pointer';
    });

    this.map.on('mouseleave', ['activations-layer', 'activated-parks-layer', 'na-parks-dynamic-layer'], () => {
      this.map.getCanvas().style.cursor = '';
    });

    this.map.on('click', 'activations-layer', (e) => {
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
      html = `<h4>${i} my activation${i > 1 ? 's' : ''}</h4><ul class='map-popup-list'>${html}</ul>`;

      this.showPopup(coordinates, html, e);
    });

    this.map.on('click', ['activated-parks-layer', 'na-parks-dynamic-layer', 'highlight-layer'], (e) => {
      //@ts-ignore
      const coordinates = e.features![0].geometry.coordinates.slice();
      this.showPopup(coordinates, this.getParkTooltipHtml(<PotaParkModel>e.features![0].properties), e);
    });

    this.map.addControl(new mapbox.NavigationControl());

    const moveEnd$ = fromEvent(this.map, 'moveend');
    const moveEndSub = moveEnd$.pipe(debounceTime(250)).subscribe({ //need to debounce when resizing a browser window
      next: () => {
        this._state.notActivatedParks.hydrated = false;
        this.hydrateAndUpdateLayers(true)
      }
    });

    this._destroyRef.onDestroy(() => {
      moveEndSub.unsubscribe();
      this.map.remove();
    });

    //this.map.on('moveend', () => {this._state.notActivatedParks.hydrated = false; this.hydrateAndUpdateLayers(true)});
  }

  private setupMapSourcesAndLayers() {
    this.map!.addSource('activations', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: []
      }
    });

    this.map!.addSource('activated-parks', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: []
      }
    });

    this.map.addSource('not-activated-parks-dynamic', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: []
      }
    });

    this.map.addSource('highlight', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: []
      }
    });

    this.map.addLayer({
      'id': 'activations-layer',
      'type': 'circle',
      'source': 'activations',
      'layout': {visibility: this.showActivations ? 'visible' : 'none'},
      'paint': {
        'circle-radius': 5,
        'circle-stroke-width': 1,
        'circle-color': '#10b981', //matches checkbox bkg
        'circle-stroke-color': '#333',
      }
    });

    this.map.addLayer({
      'id': 'activated-parks-layer',
      'type': 'circle',
      'source': 'activated-parks',
      'layout': {visibility: this.showActivations ? 'none' : 'visible'},
      'paint': {
        'circle-radius': 5,
        'circle-stroke-width': 1,
        'circle-stroke-color': '#333',
        // @ts-ignore
        'circle-color': this.getCirclePaintProperty()
      }
    });

    this.map.addLayer({
      id: 'na-parks-dynamic-layer',
      type: 'circle',
      source: 'not-activated-parks-dynamic',
      paint: {
        'circle-radius': 5,
        'circle-stroke-width': 1,
        'circle-stroke-color': '#333',
        // @ts-ignore
        'circle-color': this.getCirclePaintProperty()
      }
    });

    this.map.addLayer({
      id: 'highlight-layer',
      type: 'circle',
      source: 'highlight',
      'layout': {visibility: 'none'},
      paint: {
        'circle-radius': 10,
        'circle-stroke-width': 1,
        'circle-stroke-color': '#333',
        // @ts-ignore
        'circle-color': this.getCirclePaintProperty()
      }
    });
  }

  private hydrateAndUpdateLayers(forced: boolean) {
    this._state.notActivatedParks.hydrated = this._state.notActivatedParks.hydrated && !forced;
    this._state.activatedParks.hydrated = false;
    this._state.activations.hydrated = false;

    this.hydrateNotActivatedParks();
    this.hydrateActivatedParks();
    this.hydrateActivations();
    this.updateLayersLayout();
  }

  private hydrateNotActivatedParks() {
    if (this._state.notActivatedParks.hydrated)
      return;

    const bounds = this.map.getBounds();
    const swLong = bounds!.getWest();
    const swLat = bounds!.getSouth();
    const neLong = bounds!.getEast();
    const neLat = bounds!.getNorth();

    //when map style changes, boundary is the same - no reason to reload
    if (this._state.notActivatedParks.swLong === swLong && this._state.notActivatedParks.swLat === swLat && this._state.notActivatedParks.neLong === neLong && this._state.notActivatedParks.neLat === neLat) {
      //@ts-ignore
      this.map.getSource('not-activated-parks-dynamic').setData(this._state.notActivatedParks.data);
      this._state.notActivatedParks.hydrated = true;
      return;
    }

    this._state.notActivatedParks.swLong = swLong;
    this._state.notActivatedParks.swLat = swLat;
    this._state.notActivatedParks.neLong = neLong;
    this._state.notActivatedParks.neLat = neLat;

    // in case of already existing request. browser window resize can generate quite a few reload events.
    if (this._notActivatedParksSub)
      this._notActivatedParksSub.unsubscribe();

    this._notActivatedParksSub = this._potaSvc.getGeoJsonByBoundary(swLong, swLat, neLong, neLat).subscribe({
      next: (r) => {
        this._state.notActivatedParks.data = r;
        //@ts-ignore
        this.map.getSource('not-activated-parks-dynamic').setData(r);
        this._state.notActivatedParks.hydrated = true;
      },
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log),
      complete: () => { this._notActivatedParksSub?.unsubscribe(); this._notActivatedParksSub = undefined; }
    });
  }

  private hydrateActivatedParks() {
    if (this._state.activatedParks.hydrated || this.showActivations)
      return;

    if (this._state.activatedParks.data)
    {
      //@ts-ignore
      this.map.getSource('activated-parks').setData(this._state.activatedParks.data);
      this._state.activatedParks.hydrated = true;
      return;
    }

    this._potaSvc.getActivatedParksGeoJson().subscribe({
      next: (r) => {
        this._state.activatedParks.data = r;
        //@ts-ignore
        this.map.getSource('activated-parks').setData(r);
        this._state.activatedParks.hydrated = true;
      },
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  private hydrateActivations() {
    if (this._state.activations.hydrated || !this.showActivations)
      return;

    if (this._state.activations.data)
    {
      //@ts-ignore
      this.map.getSource('activations').setData(this._state.activations.data);
      this._state.activations.hydrated = true;
      return;
    }

    this._potaSvc.getActivationsGeoJson().subscribe({
      next: (r) => {
        this._state.activations.data = r;
        //@ts-ignore
        this.map.getSource('activations')!.setData(r);
        this._state.activations.hydrated = true;
      },
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  private updateLayersLayout() {
      this.map.setLayoutProperty('activations-layer', 'visibility', this.showActivations ? 'visible' : 'none');
      this.map.setLayoutProperty('activated-parks-layer', 'visibility', this.showActivations ? 'none' : 'visible');
  }

  private changePointColor() {
    // @ts-ignore
    this.map.setPaintProperty('na-parks-dynamic-layer', 'circle-color', this.getCirclePaintProperty());
  }

  private getCirclePaintProperty() {
    return [
      'case',
      ['<=', ['get', 'totalActivationCount'], 10], this.pointColors.first, // <= 10
      ['all', ['>', ['get', 'totalActivationCount'], 10], ['<=', ['get', 'totalActivationCount'], 20]], this.pointColors.second,
      ['all', ['>', ['get', 'totalActivationCount'], 20], ['<=', ['get', 'totalActivationCount'], 50]], this.pointColors.third,
      ['all', ['>', ['get', 'totalActivationCount'], 50], ['<=', ['get', 'totalActivationCount'], 100]], this.pointColors.fourth,
      this.pointColors.default,
    ];
  }

  private getParkTooltipHtml(p: PotaParkModel): string {
    const plural = p.totalActivationCount != 1 ? 's' : ''
    return `<div class='map-popup-na'><a href='https://pota.app/#/park/${p.parkNum}' target="_blank">${p.parkNum}</a><br>${p.parkName}<br><b>${p.totalActivationCount}</b> activation${plural}, <b>${p.totalQsoCount}</b> QSOs</div>`;
  }

  private showPopup(coordinates: any, html: string, e: mapbox.MapMouseEvent | undefined = undefined) {
    if (this._popup) this._popup.remove();

    // Ensure that if the map is zoomed out such that multiple copies of the feature are visible, the popup appears over the copy being pointed to.
    if (e && ['mercator', 'equirectangular'].includes(this.map.getProjection().name)) {
      while (Math.abs(e.lngLat.lng - coordinates[0]) > 180) {
        coordinates[0] += e.lngLat.lng > coordinates[0] ? 360 : -360;
      }
    }

    this._popup = new mapbox.Popup()
      .setLngLat(coordinates)
      .setHTML(html)
      .addTo(this.map);
  }

  onColorSelected() {
    this._colorChanges$.next();
  }

  onMapStyle(style: string) {
    if (this._state.mapStyle === style)
      return;

    this._state.mapStyle = style;
    this.map.setStyle(`mapbox://styles/mapbox/${style}-v12`);
  }

  onActivationsChecked() {
    this.hydrateAndUpdateLayers(false);
  }

  parkSearch(searchString: string) {
    this._potaSvc.searchPark(searchString).subscribe({
      next: r => this.parksFound = r,
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    })
  }

  onParkSearchSelect(p: PotaParkModel) {
    this.searchName = '';

    const coordinates = [p.long, p.lat];
    const highlightPoint = {
      'type': 'FeatureCollection',
      'features': [{
        'type': 'Feature',
        'geometry': {
          'type': 'Point',
          'coordinates': coordinates,
        },
        'properties': {
          'parkNum': p.parkNum,
          'parkName': p.parkName,
          'totalActivationCount': p.totalActivationCount,
          'totalQsoCount': p.totalQsoCount,
        }
      }]
    };

    // @ts-ignore
    this.map.getSource('highlight').setData(highlightPoint);
    this.map.setLayoutProperty('highlight-layer', 'visibility', 'visible');
    this.map.flyTo({center: <mapbox.LngLatLike>coordinates, essential: true});
    this.showPopup(coordinates, this.getParkTooltipHtml(p));
  }
}
