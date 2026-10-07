import {inject, Injectable} from '@angular/core';
import {map, Observable} from 'rxjs';
import {HamEventSummaryModel} from '../models/ham-event-summary.model';
import {Configuration} from '../shared/configuration.service';
import {HttpService} from '../shared/http.service';

type HamEventSummaryResponse = Omit<HamEventSummaryModel, 'startDate' | 'endDate'> & {
  startDate: string | null;
  endDate: string | null;
};

@Injectable({providedIn: 'root'})
export class HamEventsService {
  private _http = inject(HttpService);

  public getEvents(since: Date | null): Observable<HamEventSummaryModel[]> {
    const query = since ? `?since=${since.toISOString()}` : '';

    return this.getAndNormalizeEvents(`list${query}`);
  }

  public getActiveEvents(): Observable<HamEventSummaryModel[]> {
    return this.getAndNormalizeEvents('active');
  }

  private getAndNormalizeEvents(path: string): Observable<HamEventSummaryModel[]> {
    return this._http.get(Configuration.hamEventsUrl(path)).pipe(
      map((events: HamEventSummaryResponse[]) => events.map(event => this.normalizeEvent(event)))
    );
  }

  private normalizeEvent(event: HamEventSummaryResponse): HamEventSummaryModel {
    return {
      ...event,
      startDate: event.startDate ? new Date(event.startDate) : null,
      endDate: event.endDate ? new Date(event.endDate) : null
    };
  }
}
