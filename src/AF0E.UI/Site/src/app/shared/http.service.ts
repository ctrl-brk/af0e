import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class HttpService {
  private _http = inject(HttpClient);

  public static normalizeUrl(url: string): string {
    return url.replace('://', ':\\\\').replace('//', '/').replace(':\\\\', '://');
  }

  get(url: string): Observable<any> {
    return this._http.get(HttpService.normalizeUrl(url));
  }

  post(url: string, data?: any): Observable<any> {
    return this._http.post(HttpService.normalizeUrl(url), data);
  }

  put(url: string, data?: any): Observable<any> {
    return this._http.put(HttpService.normalizeUrl(url), data);
  }

  delete(url: string, data?: any): Observable<any> {
    return this._http.request('delete', HttpService.normalizeUrl(url), {body: data});
  }
}
