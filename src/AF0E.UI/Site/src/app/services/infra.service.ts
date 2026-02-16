import {inject, Injectable} from '@angular/core';
import {Configuration} from '../shared/configuration.service';
import {HttpService} from '../shared/http.service';
import {Observable} from 'rxjs';

@Injectable({providedIn: 'root'})
export class InfraService {
  private _http = inject(HttpService);
  private _infraUrl = Configuration.infraUrl;

  public setRigStatus(frequencyHz: number, mode: string): Observable<any> {
    console.log('setRigStatus', frequencyHz, mode);
    return this._http.post(`${this._infraUrl}/radio/status`, {frequencyHz, mode});
  }
}
