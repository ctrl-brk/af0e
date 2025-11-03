import {inject, Injectable} from '@angular/core';
import {Configuration} from '../shared/configuration.service';
import {HttpService} from '../shared/http.service';
import {map, Observable} from 'rxjs';
import {PotaActivationModel} from '../models/pota-activation.model';
import {ActivationQsoModel} from '../models/activation-qso.model';
import {PotaParkModel} from '../models/pota-park.model';
import {QsoSummaryModel} from '../models/qso-summary.model';

@Injectable({providedIn: 'root'})
export class PotaService {
  private _http = inject(HttpService);

  public getUnconfirmedLog(): Observable<QsoSummaryModel[]> {
    return this._http.get(Configuration.potaUrl(`log/unconfirmed`)).pipe(
      map((q: QsoSummaryModel[]) => {
        return q.map((s) => {
          s.date = new Date(s.date);
          return s;
        });
      })
    );
  }

  public getParkHuntingQsoSummaries(parkNum: string): Observable<QsoSummaryModel[]> {
    return this._http.get(Configuration.potaUrl(`park/${parkNum}/stats/hunting/log`)).pipe(
      map((q: QsoSummaryModel[]) => {
        return q.map((s) => {
          s.date = new Date(s.date);
          return s;
        });
      })
    );
  }

  public getActivations(): Observable<PotaActivationModel[]> {
    return this._http.get(Configuration.potaUrl('activations')).pipe(
      map((x: PotaActivationModel[]) => {
        return x.map((m) => {
          m.startDate = new Date(m.startDate);
          m.endDate = new Date(m.endDate);
          return m;
        });
      })
    );
  }

  public getActivation(id: number): Observable<PotaActivationModel> {
    return this._http.get(Configuration.potaUrl(`activations/${id}`)).pipe(
      map((m: PotaActivationModel) => {
        m.startDate = new Date(m.startDate);
        m.endDate = new Date(m.endDate);
        m.logSubmittedDate = new Date(m.logSubmittedDate);
        return m;
      })
    );
  }

  public getActivationLog(id: number): Observable<ActivationQsoModel[]> {
    return this._http.get(Configuration.potaUrl(`activations/${id}/log`)).pipe(
      map((x: ActivationQsoModel[]) => {
        return x.map((m) => {
          m.date = new Date(m.date);
          return m;
        });
      })
    );
  }

  public getActivationsGeoJson(): Observable<any> {
    return this._http.get(Configuration.potaUrl('geojson/activations/all'));
  }

  public getActivatedParksGeoJson(): Observable<any> {
    return this._http.get(Configuration.potaUrl('geojson/parks/activated'));
  }

  public getGeoJsonByBoundary(swLong: number, swLat: number, neLong: number, neLat: number): Observable<any> {
    return this._http.get(Configuration.potaUrl(`geojson/parks/not-activated/boundary?swLong=${swLong}&swLat=${swLat}&neLong=${neLong}&neLat=${neLat}`));
  }

  public searchPark(parkNum: string): Observable<PotaParkModel[]> {
    return this._http.get(Configuration.potaUrl(`parks/search/${parkNum}`)).pipe(
      map((x: PotaParkModel[]) => {
        return x.map((m) => {
          m.parkDesc = `${m.parkNum} - ${m.parkName}`;
          return m;
        })
      })
    );
  }

  public getPark(parkNum: string): Observable<PotaParkModel> {
    return this._http.get(Configuration.potaUrl(`park/${parkNum}`)).pipe(
      map((x: PotaParkModel) => {
          x.parkDesc = `${x.parkNum} - ${x.parkName}`;
          return x;
        })
    );
  }
}
