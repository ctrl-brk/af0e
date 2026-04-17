import {inject, Injectable} from '@angular/core';
import {Configuration} from '../shared/configuration.service';
import {HttpService} from '../shared/http.service';
import {map, Observable} from 'rxjs';
import {PotaAppSpotHistoryModel} from '../models/pota-app-spot-history.model';

@Injectable({providedIn: 'root'})
export class PotaAppService {
  private _http = inject(HttpService);
  private _url = Configuration.potaAppUrl;

  public addSpot(call: string, reference: string, frequency: string, mode: string | undefined, comments: string): Observable<any> {
    return this._http.post(`${this._url}/spot`, {activator: call, spotter: 'AF0E', source: 'af0e.org', reference, frequency, mode, comments});
  }

  public getSpotHistory(reference: string): Observable<PotaAppSpotHistoryModel[]> {
    return this._http.get(`${this._url}/v1/spots/AF0E/${reference}`).pipe(
      map((x: PotaAppSpotHistoryModel[]) => {
        return x.map((m) => {
          m.spotTime = new Date(m.spotTime);
          m.source = m.source.startsWith('Ham2K P') ? 'PoLo' : m.source;
          return m;
        })
      })
    );
  }
}
