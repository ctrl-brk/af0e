import {inject, Injectable, NgZone} from '@angular/core';
import {firstValueFrom, map, Observable, Subject} from 'rxjs';
import * as signalR from '@microsoft/signalr';
import {AuthService} from '@auth0/auth0-angular';
import {Configuration} from '../shared/configuration.service';
import {LogService} from '../shared/log.service';
import {environment} from '../../environments/environment';
import {HttpService} from '../shared/http.service';
import {DxClusterSpotModel} from '../models/dx-cluster-spot.model';
import {DxClusterStatusModel} from '../models/dx-cluster-status.model';

@Injectable({providedIn: 'root'})
export class DxClusterService {
  private _auth = inject(AuthService);
  private _zone = inject(NgZone);
  private _log = inject(LogService);
  private _http = inject(HttpService);
  private _hub?: signalR.HubConnection;
  private _subscribed = false;

  readonly status$ = new Subject<DxClusterStatusModel>();
  readonly spot$ = new Subject<DxClusterSpotModel>();

  public getStatus(): Observable<DxClusterStatusModel> {
    return this._http.get(Configuration.dxClusterUrl('status')).pipe(map((status: DxClusterStatusModel) => this.normalizeStatus(status)));
  }

  public getSpots(since?: Date, filterName?: string | null): Observable<DxClusterSpotModel[]> {
    const query: string[] = [];

    if (since)
      query.push(`since=${encodeURIComponent(since.toISOString())}`);

    if (filterName)
      query.push(`filter=${encodeURIComponent(filterName)}`);

    const url = query.length > 0
      ? `${Configuration.dxClusterUrl('spots')}?${query.join('&')}`
      : `${Configuration.dxClusterUrl('spots')}`;

    return this._http.get(url).pipe(map((spots: DxClusterSpotModel[]) => spots.map(spot => this.normalizeSpot(spot))));
  }

  public async subscribeDxCluster(): Promise<DxClusterStatusModel> {
    await this.ensureConnected();

    const status = this.normalizeStatus(await this._hub!.invoke<DxClusterStatusModel>('SubscribeDxCluster'));
    this._subscribed = true;
    this.status$.next(status);

    return status;
  }

  public async unsubscribeDxCluster(): Promise<void> {
    this._subscribed = false;

    if (this._hub?.state !== signalR.HubConnectionState.Connected)
      return;

    await this._hub.invoke('UnsubscribeDxCluster');
  }

  private async ensureConnected(): Promise<void> {
    if (this._hub?.state === signalR.HubConnectionState.Connected || this._hub?.state === signalR.HubConnectionState.Connecting)
      return;

    if (!this._hub)
      this._hub = this.buildConnection();

    await this._hub.start();
  }

  private buildConnection(): signalR.HubConnection {
    const builder = new signalR.HubConnectionBuilder()
      .withUrl(Configuration.dxClusterHubUrl(), {
        accessTokenFactory: async () => {
          const isAuthenticated = await firstValueFrom(this._auth.isAuthenticated$);
          if (!isAuthenticated)
            return '';

          try {
            return await firstValueFrom(this._auth.getAccessTokenSilently());
          } catch (err) {
            this._log.warn('DX cluster SignalR token acquisition failed; connecting without token', err);
            return '';
          }
        }
      })
      .withAutomaticReconnect();

    if (!environment.production)
      builder.configureLogging(signalR.LogLevel.Information);

    const hub = builder.build();

    hub.onreconnecting(err => this._log.warn('DX cluster SignalR reconnecting', err));
    hub.onreconnected(async id => {
      this._log.debug('DX cluster SignalR reconnected', id);

      if (!this._subscribed)
        return;

      try {
        const status = this.normalizeStatus(await hub.invoke<DxClusterStatusModel>('SubscribeDxCluster'));
        this._zone.run(() => this.status$.next(status));
      } catch (err) {
        this._log.error(err);
      }
    });
    hub.onclose(err => this._log.error(err));

    hub.on('dxcluster.status', (status: DxClusterStatusModel) => {
      const normalized = this.normalizeStatus(status);
      this._zone.run(() => this.status$.next(normalized));
    });

    hub.on('dxcluster.spot', (spot: DxClusterSpotModel) => {
      const normalized = this.normalizeSpot(spot);
      this._zone.run(() => this.spot$.next(normalized));
    });

    return hub;
  }

  private normalizeStatus(status: DxClusterStatusModel): DxClusterStatusModel {
    return {
      ...status,
      lastAccessUtc: this.parseDate(status.lastAccessUtc),
      lastStartUtc: this.parseDate(status.lastStartUtc),
      lastStopUtc: this.parseDate(status.lastStopUtc),
      filters: (status.filters ?? []).map(filter => ({
        ...filter,
        callsignPatterns: filter.callsignPatterns ?? '',
        modes: [...(filter.modes ?? [])],
        invalidCallsignPatterns: [...(filter.invalidCallsignPatterns ?? [])],
        frequencyWindows: (filter.frequencyWindows ?? []).map(window => ({
          minFrequencyKhz: window.minFrequencyKhz ?? null,
          maxFrequencyKhz: window.maxFrequencyKhz ?? null,
        })),
      })),
      servers: (status.servers ?? []).map(server => ({
        ...server,
        lastConnectUtc: this.parseDate(server.lastConnectUtc),
        lastDisconnectUtc: this.parseDate(server.lastDisconnectUtc),
        lastLineUtc: this.parseDate(server.lastLineUtc),
        lastSpotUtc: this.parseDate(server.lastSpotUtc),
        lastErrorUtc: this.parseDate(server.lastErrorUtc)
      }))
    };
  }

  private normalizeSpot(spot: DxClusterSpotModel): DxClusterSpotModel {
    return {
      ...spot,
      dxccEntityCode: spot.dxccEntityCode ?? null,
      dxccEntityName: spot.dxccEntityName ?? null,
      dxccCountryCode: spot.dxccCountryCode ?? null,
      dxccWorkedStatus: spot.dxccWorkedStatus ?? null,
      mode: spot.mode ?? null,
      spotTimeUtc: new Date(spot.spotTimeUtc),
      receivedAtUtc: new Date(spot.receivedAtUtc)
    };
  }

  private parseDate(value: Date | string | null | undefined): Date | null {
    return value ? new Date(value) : null;
  }
}
