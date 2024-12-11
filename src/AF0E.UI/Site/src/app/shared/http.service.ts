import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable, throwError} from 'rxjs';
import {catchError} from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class HttpService {
  private _http = inject(HttpClient);

  public static normalizeUrl(url: string): string {
    return url.replace('://', ':\\\\').replace('//', '/').replace(':\\\\', '://');
  }

  private static handleError(error: any) {
    // this method gets called by reference, no matter if it's a class member or not, so no access to other class members. 'this.' is actually a type of CatchSubscriber
    console.warn(error);
    if (error.status === 0)
      alert("Connection error. Please make sure the back-end service is accessible.");
    // else if (error.status === 401)
    //     alert("Not authorized");
    // else if (error.status === 403)
    //     alert("Access denied");

    return throwError(() => new Error(error.message || 'Server error'));
  }

  get(url: string): Observable<any> {
    return this._http.get(HttpService.normalizeUrl(url)).pipe(catchError(HttpService.handleError));
  }

  post(url: string, data?: any): Observable<any> {
    return this._http.post(HttpService.normalizeUrl(url), data).pipe(catchError(HttpService.handleError));
  }

  put(url: string, data?: any): Observable<any> {
    return this._http.put(HttpService.normalizeUrl(url), data).pipe(catchError(HttpService.handleError));
  }

  delete(url: string, data?: any): Observable<any> {
    return this._http.request('delete', HttpService.normalizeUrl(url), {body: data}).pipe(catchError(HttpService.handleError));
  }
}
