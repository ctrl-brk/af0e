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
    let msg = 'Server error. Please contact your system administrator.';
    const severity = NotificationMessageSeverity.Error;
    let title = 'Error';
    let sticky = false;

    if (!(r instanceof HttpErrorResponse)) {
      msg = 'Unhandled error. Please contact your system administrator.';
      if (log) log.error(r);
    } else if (r.status === 0) //handled by http helper
      return;
    else if (r.status >= 500) {
      ntf.addMessage(new NotificationMessageModel(severity, title, msg, sticky));
      return;
    } else if (r.status === 401) {
      title = "Not authorized";
      msg = "You are not authorized to use this resource. Please contact your system administrator.";
      sticky = true;
    } else if (r.status === 403) {
      title = "Access denied";
      msg = "You do not have permissions to access this feature. Please contact your system administrator.";
      sticky = true;
    } else if (r.status === 404) {
      title = "Not found";
      msg = "The server resource is not found. Please contact your system administrator.";
      sticky = true;
    } else {
      const err: ErrorDtoModel = r.error;

      if (err.source === 3) { //ErrorSource.Business
        msg = err.message;
        if (err.severity === ErrorSeverity.Conflict)
          sticky = true;
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
}
