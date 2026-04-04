import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {map, Observable} from 'rxjs';
import {environment} from '../../environments/environment';

export interface ReverseGeocodeLocation {
  formattedAddress: string | null;
  addressLine1: string | null;
  city: string | null;
  county: string | null;
  state: string | null;
  stateCode: string | null;
  postalCode: string | null;
  country: string | null;
  countryCode: string | null;
}

export interface MapboxGeocodingResponse {
  type: 'FeatureCollection';
  features: MapboxFeature[];
}

export interface MapboxFeature {
  id: string;
  type: 'Feature';
  geometry?: {
    type: 'Point';
    coordinates: [number, number];
  };
  properties?: MapboxFeatureProperties;
}

export interface MapboxFeatureProperties {
  mapbox_id?: string;
  feature_type?: string;
  full_address?: string;
  name?: string;
  name_preferred?: string;
  place_formatted?: string;
  context?: MapboxContext;
}

export interface MapboxContext {
  address?: MapboxContextAddress;
  street?: MapboxContextItem;
  postcode?: MapboxContextItem;
  place?: MapboxContextItem;
  district?: MapboxContextItem;
  county?: MapboxContextItem;
  region?: MapboxRegionContextItem;
  country?: MapboxCountryContextItem;
}

export interface MapboxContextItem {
  mapbox_id?: string;
  name?: string;
}

export interface MapboxContextAddress extends MapboxContextItem {
  address_number?: string;
  street_name?: string;
}

export interface MapboxRegionContextItem extends MapboxContextItem {
  region_code?: string;
  region_code_full?: string;
}

export interface MapboxCountryContextItem extends MapboxContextItem {
  country_code?: string;
  country_code_alpha_3?: string;
}

@Injectable({providedIn: 'root'})
export class MapboxService {
  private readonly http = inject(HttpClient);
  private readonly accessToken = environment.mapBoxKey;
  private readonly baseUrl = 'https://api.mapbox.com/search/geocode/v6/reverse';

  reverseGeocode(latitude: number, longitude: number): Observable<MapboxGeocodingResponse> {
    const params = new HttpParams()
      .set('latitude', latitude)
      .set('longitude', longitude)
      .set('access_token', this.accessToken)
      .set('limit', 1)
      .set('language', 'en');

    return this.http.get<MapboxGeocodingResponse>(this.baseUrl, { params });
  }

  getLocationByCoordinates(latitude: number, longitude: number): Observable<ReverseGeocodeLocation> {
    return this.reverseGeocode(latitude, longitude).pipe(
      map(response => {
        const feature = response.features?.[0];
        const props = feature?.properties;
        const context = props?.context;

        const rawCounty =
          context?.district?.name ??
          context?.county?.name ??
          null;

        const county = rawCounty?.replace(/\s+county$/i, '') ?? null;

        const state =
          context?.region?.name ??
          null;

        const stateCode =
          context?.region?.region_code ??
          context?.region?.region_code_full?.replace(/^[A-Z]{2}-/, '') ??
          null;

        const postalCode =
          context?.postcode?.name ??
          null;

        const city =
          context?.place?.name ??
          null;

        const country =
          context?.country?.name ??
          null;

        const countryCode =
          context?.country?.country_code?.toUpperCase() ??
          null;

        const addressNumber =
          context?.address?.address_number ??
          null;

        const streetName =
          context?.address?.street_name ??
          context?.street?.name ??
          null;

        const addressLine1 =
          addressNumber && streetName
            ? `${addressNumber} ${streetName}`
            : streetName ?? props?.name ?? null;

        const formattedAddress =
          props?.full_address ??
          null;

        return {
          formattedAddress,
          addressLine1,
          city,
          county,
          state,
          stateCode,
          postalCode,
          country,
          countryCode
        };
      })
    );
  }
}
