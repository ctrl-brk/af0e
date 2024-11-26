import {Component, inject, OnInit} from '@angular/core';
import {RouterOutlet} from '@angular/router';
import {Subscription} from 'rxjs';
import {MessageService} from 'primeng/api';
import {ToastModule} from 'primeng/toast';
import {FooterComponent} from './footer/footer.component';
import {HeaderComponent} from './header/header.component';
import {HomeComponent} from './home/home.component';
import {NotificationService} from '../shared/notification.service';
import {NotificationMessageSeverity} from '../shared/notification-message.model';
import {PrimeNG} from 'primeng/config';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    FooterComponent,
    HeaderComponent,
    HomeComponent,
    ToastModule,
    RouterOutlet
  ],
  styles: `
    :host {
      flex: 1;
      display: flex;
      flex-direction: column;
    }
    .wrapper {
    flex: 1;
    display: flex;
    flex-direction: column;
    }
  `,
  template: `
    <div class='wrapper'>
      <app-header></app-header>
      <router-outlet/>
    </div>
    <app-footer></app-footer>
    <p-toast [life]="5000"></p-toast>
  `
})
export class AppComponent implements OnInit {
  private _msgSvc = inject(MessageService);
  private _pNg = inject(PrimeNG);
  private _ntfSvc = inject(NotificationService);
  private _ntfSub?: Subscription;

  ngOnInit() {
    // this._pNg.zIndex = {
    //   modal: 1100,    // dialog, sidebar
    //   overlay: 1000,  // dropdown, overlaypanel
    //   menu: 1000,     // overlay menus
    //   tooltip: 1100   // tooltip
    // };

    this._ntfSub = this._ntfSvc.messages$.subscribe(
      msg => {
        let sev: string;

        switch (msg.severity) {
          case NotificationMessageSeverity.Success :
            sev = 'success';
            break;
          case NotificationMessageSeverity.Info :
            sev = 'info';
            break;
          case NotificationMessageSeverity.Warn :
            sev = 'warn';
            break;
          case NotificationMessageSeverity.Error :
            sev = 'error';
            break;
        }

        this._msgSvc.add({severity: sev, summary: msg.summary, detail: msg.message, sticky: msg.sticky});
      }
    );
  }
}
