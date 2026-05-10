import {inject, Injectable} from '@angular/core';
import {Configuration} from '../shared/configuration.service';
import {HttpService} from '../shared/http.service';
import {map, Observable} from 'rxjs';
import {KeyerStatusModel} from '../models/keyer-status.model';
import {RadioStatus} from '../models/radio-status.model';

@Injectable({providedIn: 'root'})
export class InfraService {
  private _http = inject(HttpService);
  private _infraUrl = Configuration.infraUrl;

  public getRigStatus(): Observable<RadioStatus> {
    return this._http.get(`${this._infraUrl}/radio/status`);
  }

  public setRigStatus(frequencyHz: number, mode: string, filter?: number): Observable<any> {
    return this._http.post(`${this._infraUrl}/radio/status`, {frequencyHz, mode, filter: filter ? filter : null});
  }

  public setNoiseReduction(enabled: boolean): Observable<any> {
    return this._http.post(`${this._infraUrl}/radio/nr`, {enabled: enabled});
  }

  public setNoiseBlanket(enabled: boolean): Observable<any> {
    return this._http.post(`${this._infraUrl}/radio/nb`, {enabled: enabled});
  }

  //speed = 0 resets to pot speed
  public sendCw(text: string, rigControl: boolean, wpm: number|null, repeat: number|null, repeatDelaySeconds: number|null): Observable<any> {
    return this._http.post(`${this._infraUrl}/winkeyer/send`, {text, rigControl, wpm, repeat, repeatDelaySeconds});
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

  public saveAdif(adif: string): Observable<any> {
    return this._http.post(`${this._infraUrl}/log/adif`, {adif});
  }

  public getConfig(): Observable<any> {
    return this._http.get(`${this._infraUrl}/config`);
  }
}
