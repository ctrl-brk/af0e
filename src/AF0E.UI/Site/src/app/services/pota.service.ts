import {inject, Injectable} from '@angular/core';
import {Configuration} from '../shared/configuration.service';
import {HttpService} from '../shared/http.service';
import {map, Observable} from 'rxjs';
import {PotaActivationModel} from '../models/pota-activation.model';
import {ActivationQsoModel} from '../models/activation-qso.model';

@Injectable({providedIn: 'root'})
export class PotaService {
  private _http = inject(HttpService);

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
}
