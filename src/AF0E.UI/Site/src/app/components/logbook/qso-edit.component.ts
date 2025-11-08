import {Component, effect, inject, input, ViewEncapsulation} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Card} from 'primeng/card';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {LogbookService} from '../../services/logbook.service';
import {QsoDetailModel} from '../../models/qso-detail.model';
import {Utils} from '../../shared/utils';

@Component({
  selector: 'app-qso-edit',
  templateUrl: './qso-edit.component.html',
  styleUrl: './qso-edit.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    Card,
    FormsModule,
  ],
})
export class QsoEditComponent {
  private _lbSvc = inject(LogbookService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  logId = input.required<number>();
  protected qso: QsoDetailModel = null!;

  constructor() {
    effect(() => {
      this.onQsoChange(this.logId());
    });
  }

  private onQsoChange(id: number) {
    this._lbSvc.getQso(id).subscribe({
      next: (r: QsoDetailModel) => this.qso = r,
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  onTabChange(tab: unknown) {
  }
}
