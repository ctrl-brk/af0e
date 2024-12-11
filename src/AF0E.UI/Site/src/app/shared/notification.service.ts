import {Injectable} from "@angular/core";
import {Subject} from 'rxjs';

import {NotificationMessageModel} from "./notification-message.model";

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
    private _messageSource = new Subject<NotificationMessageModel>();
    public messages$ = this._messageSource.asObservable();

    public addMessage(msg: NotificationMessageModel) {
        this._messageSource.next(msg);
    }
}
