import {HttpErrorResponse} from '@angular/common/http';
import {NotificationService} from './notification.service';
import {LogService} from './log.service';
import {NotificationMessageModel, NotificationMessageSeverity} from './notification-message.model';
import {ErrorSeverity} from './error-severity.enum';
import {ErrorDtoModel} from './error-dto.model';

export class Utils {

  public static handleBackendError(r: HttpErrorResponse, ntf?: NotificationService, log?: LogService): string | undefined {
    if (r.status >= 500) {
      if (ntf)
        this.showErrorMessage(r, ntf, log);
      return;
    }

    if (r.status === 401) return 'Not authorized';
    if (r.status === 403) return 'Access denied';

    if (r.error.source !== 3) //ErrorSource.Business
      this.showErrorMessage(r, ntf!, log);
    else
      return r.error.message;

    return;
  }

  public static showErrorMessage(r: any, ntf: NotificationService, log?: LogService) {
    let msg = 'Server error. Please notify me.';
    const severity = NotificationMessageSeverity.Error;
    let title = 'Error';
    let sticky = false;

    if (!(r instanceof HttpErrorResponse)) {
      msg = 'Unhandled error. Please notify me.';
      if (log) log.error(r);
    } else if (r.status === 0) //handled by http helper
      return;
    else if (r.status >= 500) {
      ntf.addMessage(new NotificationMessageModel(severity, title, msg, sticky));
      return;
    } else if (r.status === 401) {
      title = "Not authorized";
      msg = "You are not authorized to use this resource.";
      sticky = true;
    } else if (r.status === 403) {
      title = "Access denied";
      msg = "You do not have permissions to access this feature.";
      sticky = true;
    } else if (r.status === 404) {
      title = "Not found";
      msg = "The resource is not found. Please notify me.";
      sticky = true;
    } else {
      if (r.error) {
        const err: ErrorDtoModel = r.error;

        if (err.source === 3) { //ErrorSource.Business
          msg = err.message;
          if (err.severity === ErrorSeverity.Conflict)
            sticky = true;
        }
      }
      else {
        title = "Error";
        msg = "Unexpected error. Please notify me.";
      }
    }

    ntf.addMessage(new NotificationMessageModel(severity, title, msg, sticky));
  }

  public static dateToYmd(date?: Date | string | null): string {
    if (!date) return '';

    if (typeof(date) === 'string')
      date = new Date(date);

    return date.getUTCFullYear() + "-" + ("0" + (date.getUTCMonth() + 1)).slice(-2) + "-" + ("0" + date.getUTCDate()).slice(-2);
  }

  public static dateToSql(date: Date): string {
    return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`;
  }

  public static getBandFromFrequency(freqMHz: number): string | null {
    if (freqMHz >= 1.8 && freqMHz <= 2.0) return '160m';
    if (freqMHz >= 3.5 && freqMHz <= 4.0) return '80m';
    if (freqMHz >= 5.3 && freqMHz <= 5.4) return '60m';
    if (freqMHz >= 7.0 && freqMHz <= 7.3) return '40m';
    if (freqMHz >= 10.1 && freqMHz <= 10.15) return '30m';
    if (freqMHz >= 14.0 && freqMHz <= 14.35) return '20m';
    if (freqMHz >= 18.068 && freqMHz <= 18.168) return '17m';
    if (freqMHz >= 21.0 && freqMHz <= 21.45) return '15m';
    if (freqMHz >= 24.89 && freqMHz <= 24.99) return '12m';
    if (freqMHz >= 28.0 && freqMHz <= 29.7) return '10m';
    if (freqMHz >= 50.0 && freqMHz <= 54.0) return '6m';
    if (freqMHz >= 144.0 && freqMHz <= 148.0) return '2m';
    if (freqMHz >= 420.0 && freqMHz <= 450.0) return '70cm';

    return null;
  }

  public static getCurrentUtcDate(): Date {
    const now = new Date();
    // Create a "local" date that displays the current UTC time
    // This allows the DatePicker to show the UTC time without timezone shifts
    return new Date(
      now.getUTCFullYear(),
      now.getUTCMonth(),
      now.getUTCDate(),
      now.getUTCHours(),
      now.getUTCMinutes(),
      now.getUTCSeconds()
    );
  }

  /**
   * Converts a Date object to ISO 8601 UTC string format for API submission
   * This treats the "local" date components as UTC to avoid double conversion
   */
  public static dateToUtcString(date: Date | null | undefined): string | null {
    if (!date) return null;

    // Read the date components as if they are already UTC values
    // and construct an ISO string manually
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');

    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}Z`;
  }

  /**
   * Converts a UTC date string from the API to a Date object for form display
   * This creates a "local" Date that displays the UTC values without timezone shift
   */
  public static utcStringToDate(dateString: string | Date | null | undefined): Date | null {
    if (!dateString) return null;

    // If already a Date object, return as-is
    if (dateString instanceof Date) return dateString;

    // Parse the UTC date string and extract components
    const date = new Date(dateString);

    // Create a "local" date with the UTC components
    // This prevents the DatePicker from shifting the display
    return new Date(
      date.getUTCFullYear(),
      date.getUTCMonth(),
      date.getUTCDate(),
      date.getUTCHours(),
      date.getUTCMinutes(),
      date.getUTCSeconds()
    );
  }
}
