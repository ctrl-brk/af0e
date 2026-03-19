import {environment} from '../../environments/environment';

export class Configuration {
  private static _apiUrl = environment.apiUrl;
  private static _logbookUrl = `${Configuration._apiUrl}/logbook`;
  private static _gridtrackerUrl = `${Configuration._apiUrl}/gridtracker`;
  private static _potaUrl = `${Configuration._apiUrl}/pota`;
  private static _qrzUrl = `${Configuration._apiUrl}/qrz`;
  private static _utilsUrl = `${Configuration._apiUrl}/tools`;
  private static _notificationUrl = `${Configuration._apiUrl}/notification`;
  public static infraUrl = environment.infraUrl;

  public static get LoadingDelay(): number {
    return 200;
  }

  public static apiUrl(url: string): string {
    return `${this._apiUrl}/${url}`;
  }

  public static logbookUrl(url?: string): string {
    return url === undefined ? this._logbookUrl : `${this._logbookUrl}/${url}`;
  }

  public static gridtrackerUrl(url?: string): string {
    return url === undefined ? this._gridtrackerUrl : `${this._gridtrackerUrl}/${url}`;
  }

  public static potaUrl(url?: string): string {
    return url === undefined ? this._potaUrl : `${this._potaUrl}/${url}`;
  }

  public static qrzUrl(url?: string): string {
    return url === undefined ? this._qrzUrl : `${this._qrzUrl}/${url}`;
  }

  public static utilsUrl(url?: string): string {
    return url === undefined ? this._utilsUrl : `${this._utilsUrl}/${url}`;
  }

  public static notificationUrl(url: string): string {
    return `${this._notificationUrl}/${url}`;
  }
}
