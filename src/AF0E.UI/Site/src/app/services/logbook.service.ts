import {inject, Injectable} from '@angular/core';
import {map, Observable} from 'rxjs';
import {HttpService} from '../shared/http.service';
import {Configuration} from '../shared/configuration.service';
import {SortDirection} from '../shared/sort-direction.enum';
import {QsoDetailModel} from '../models/qso-detail.model';
import {Utils} from '../shared/utils';
import {LogSearchResponseModel} from '../models/logsearch-response.model';

@Injectable({providedIn: 'root'})
export class LogbookService {
  private _svcUrl = Configuration.logbookUrl();
  private _http = inject(HttpService);

  public lookupPartial(call: string): Observable<string[]> {
    return this._http.get(`${this._svcUrl}/partial-lookup/${encodeURIComponent(call)}`);
  }

  public getQsoSummaries(call: string | null, skip: number, take: number, order: SortDirection, sortBy: string, dateRange: Date[]): Observable<LogSearchResponseModel> {
    const sortOrder = order === SortDirection.Ascending ? 1 : 0;
    const url = `${this._svcUrl}${(call === null || call === undefined) ? '' : '/' + encodeURIComponent(call)}?skip=${skip}&take=${take}&orderBy=${sortOrder}&begin=${Utils.dateToSql(dateRange[0])}&end=${Utils.dateToSql(dateRange[1])}`
    return this._http.get(url).pipe(
      map((q: LogSearchResponseModel) => {
        q.contacts.forEach(c => c.date = new Date(c.date));
        return q;
      })
    );
  }

  public getQso(id: number): Observable<QsoDetailModel> {
    return this._http.get(`${this._svcUrl}/qso/${id}`).pipe(
      map((q: QsoDetailModel) => {
        q.date = new Date(q.date);
        return q;
      })
    );
  }

  public createQso(qso: QsoDetailModel): Observable<QsoDetailModel> {
    return this._http.post(`${this._svcUrl}/qso`, qso);
  }

  public updateQso(qso: QsoDetailModel): Observable<QsoDetailModel> {
    return this._http.put(`${this._svcUrl}/qso`, qso);
  }
}
