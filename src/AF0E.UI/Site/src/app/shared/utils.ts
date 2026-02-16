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

  public static getBandFromFrequency(freqHz: number | null): string | null {
    if (!freqHz) return null;

    if (freqHz >= 1800000 && freqHz <= 2000000) return '160m';
    if (freqHz >= 3500000 && freqHz <= 4000000) return '80m';
    if (freqHz >= 5330500 && freqHz <= 5403500) return '60m';
    if (freqHz >= 7000000 && freqHz <= 7300000) return '40m';
    if (freqHz >= 10100000 && freqHz <= 10150000) return '30m';
    if (freqHz >= 14000000 && freqHz <= 14350000) return '20m';
    if (freqHz >= 18068000 && freqHz <= 18168000) return '17m';
    if (freqHz >= 21000000 && freqHz <= 21450000) return '15m';
    if (freqHz >= 24890000 && freqHz <= 24990000) return '12m';
    if (freqHz >= 28000000 && freqHz <= 29700000) return '10m';
    if (freqHz >= 50000000 && freqHz <= 54000000) return '6m';
    if (freqHz >= 144000000 && freqHz <= 148000000) return '2m';
    if (freqHz >= 222000000 && freqHz <= 225000000) return '1.25m';
    if (freqHz >= 420000000 && freqHz <= 450000000) return '70cm';

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
