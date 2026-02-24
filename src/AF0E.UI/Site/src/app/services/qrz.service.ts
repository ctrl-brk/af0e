import {inject, Injectable} from '@angular/core';
import {HttpService} from '../shared/http.service';
import {Observable} from 'rxjs';
import {Configuration} from '../shared/configuration.service';
import {QrzDetailsModel} from '../models/qrz-details.model';

@Injectable({providedIn: 'root'})
export class QrzService {
  private _http = inject(HttpService);

  public lookup(call: string): Observable<QrzDetailsModel> {
    return this._http.get(Configuration.qrzUrl(`${encodeURIComponent(call)}`));
  }
}
