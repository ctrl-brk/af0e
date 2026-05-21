import {inject, Injectable} from '@angular/core';
import {map, Observable} from 'rxjs';
import {HttpService} from '../shared/http.service';
import {Configuration} from '../shared/configuration.service';
import {SortDirection} from '../shared/sort-direction.enum';
import {QsoDetailModel} from '../models/qso-detail.model';
import {AdifDetailsModel} from '../models/adif-details.model';
import {Utils} from '../shared/utils';
import {LogSearchResponseModel} from '../models/logsearch-response.model';
import {GridTrackerLookupModel} from '../models/gridtracker-lookup.model';
import {AdifImportResponseModel} from '../models/adif-import-response.model';
import {LotwSyncResponseModel} from '../models/lotw-sync-response.model';

@Injectable({providedIn: 'root'})
export class LogbookService {
  private _svcUrl = Configuration.logbookUrl();
  private _gridtrackerUrl = Configuration.gridtrackerUrl();
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

  public getForAdif(call: string | null, dateRange: Date[]): Observable<AdifDetailsModel[]> {
    const url = `${this._svcUrl}/adif/${(call === null || call === undefined) ? '' : '/' + encodeURIComponent(call)}?&begin=${Utils.dateToSql(dateRange[0])}&end=${Utils.dateToSql(dateRange[1])}`
    return this._http.get(url).pipe(
      map((x: AdifDetailsModel[]) => {
        return x.map((m) => {
          m.date = new Date(m.date);
          return m;
        });
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

  public getGridTrackerLog(call: string): Observable<GridTrackerLookupModel[]> {
    return this._http.get(`${this._gridtrackerUrl}/${call}`).pipe(
      map((x: GridTrackerLookupModel[]) => {
        return x.map((m) => {
          m.date = new Date(m.date);
          return m;
        });
      })
    );
  }

  public createQso(potaActivationId: number|null, qso: QsoDetailModel): Observable<QsoDetailModel> {
    return this._http.post(`${this._svcUrl}/qso`, {potaActivationId, qso});
  }

  public updateQso(qso: QsoDetailModel): Observable<QsoDetailModel> {
    return this._http.put(`${this._svcUrl}/qso`, qso);
  }

  public deleteQso(id: number): Observable<any> {
    return this._http.delete(`${this._svcUrl}/qso/${id}`);
  }

  public uploadAdif(activationId: number, file: File): Observable<AdifImportResponseModel> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('activationId', activationId.toString());
    return this._http.post(`${this._svcUrl}/upload`, formData);
  }

  public lotwDownload(date: Date): Observable<LotwSyncResponseModel> {
    const formattedDate = new Intl.DateTimeFormat('en-CA').format(date);
    return this._http.post(`${this._svcUrl}/lotw/qsls`, {date: formattedDate});
  }
}
