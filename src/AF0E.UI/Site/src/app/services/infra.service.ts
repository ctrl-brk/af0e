import {inject, Injectable} from '@angular/core';
import {Configuration} from '../shared/configuration.service';
import {HttpService} from '../shared/http.service';
import {map, Observable} from 'rxjs';
import {KeyerStatusModel} from '../models/keyer-status.model';

@Injectable({providedIn: 'root'})
export class InfraService {
  private _http = inject(HttpService);
  private _infraUrl = Configuration.infraUrl;

  public setRigStatus(frequencyHz: number, mode: string): Observable<any> {
    return this._http.post(`${this._infraUrl}/radio/status`, {frequencyHz, mode});
  }

  public sendCw(text: string): Observable<any> {
    return this._http.post(`${this._infraUrl}/winkeyer/send`, {text});
  }

  public cancelCw(): Observable<any> {
    return this._http.get(`${this._infraUrl}/winkeyer/abort`);
  }

  public getKeyerStatus(): Observable<KeyerStatusModel> {
    return this._http.get(`${this._infraUrl}/winkeyer/status`).pipe(
      map((x: KeyerStatusModel) => {
        return x;
      })
    );
  }
}
