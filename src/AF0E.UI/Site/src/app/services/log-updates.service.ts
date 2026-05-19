import {inject, Injectable, NgZone} from '@angular/core';
import {firstValueFrom, Subject} from 'rxjs';
import * as signalR from '@microsoft/signalr';
import {AuthService} from '@auth0/auth0-angular';
import {Configuration} from '../shared/configuration.service';
import {LogService} from '../shared/log.service';
import {environment} from '../../environments/environment';

export interface LogChangedEvent {
  operation: 'created' | 'updated' | 'deleted' | 'imported';
  logId?: number;
  activationId?: number;
  call?: string;
  source: string;
  occurredUtc: string;
  version: number;
}

@Injectable({providedIn: 'root'})
export class LogUpdatesService {
  private _auth = inject(AuthService);
  private _zone = inject(NgZone);
  private _log = inject(LogService);
  private _hub?: signalR.HubConnection;
  private _seenVersions = new Set<number>();

  readonly changed$ = new Subject<LogChangedEvent>();

  async ensureConnected(): Promise<void> {
    if (this._hub?.state === signalR.HubConnectionState.Connected || this._hub?.state === signalR.HubConnectionState.Connecting)
      return;

    if (!this._hub)
      this._hub = this.buildConnection();

    await this._hub.start();
  }

  private buildConnection(): signalR.HubConnection {
    const builder = new signalR.HubConnectionBuilder()
      .withUrl(Configuration.logbookHubUrl(), {
        accessTokenFactory: async () => {
          const isAuthenticated = await firstValueFrom(this._auth.isAuthenticated$);
          if (!isAuthenticated)
            return '';

          try {
            return await firstValueFrom(this._auth.getAccessTokenSilently());
          } catch (err) {
            this._log.warn('SignalR token acquisition failed; connecting without token', err);
            return '';
          }
        }
      })
      .withAutomaticReconnect();

    if (!environment.production)
      builder.configureLogging(signalR.LogLevel.Information);

    const hub = builder.build();

    hub.onreconnecting(err => this._log.warn('SignalR reconnecting', err));
    hub.onreconnected(id => this._log.debug('SignalR reconnected', id));
    hub.onclose(err => this._log.error(err));

    hub.on('log.changed', (evt: LogChangedEvent) => {
      if (!evt?.version)
        return;

      if (this._seenVersions.has(evt.version))
        return;

      this._seenVersions.add(evt.version);

      if (this._seenVersions.size > 1000) {
        const recent = [...this._seenVersions].sort((a, b) => b - a).slice(0, 500);
        this._seenVersions = new Set<number>(recent);
      }

      this._zone.run(() => this.changed$.next(evt));
    });

    return hub;
  }
}


