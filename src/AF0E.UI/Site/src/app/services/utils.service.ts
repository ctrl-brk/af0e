import {inject, Injectable} from '@angular/core';
import {HttpService} from '../shared/http.service';
import {Observable} from 'rxjs';
import {Configuration} from '../shared/configuration.service';

@Injectable({providedIn: 'root'})
export class UtilsService {
  private _http = inject(HttpService);

  public coordinatesToGrid(lat: string, lng: string): Observable<string> {
    return this._http.get(Configuration.utilsUrl(`grid?latitude=${lat}&longitude=${lng}`));
  }
}
