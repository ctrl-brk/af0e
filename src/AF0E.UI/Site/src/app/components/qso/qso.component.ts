import {Component, effect, inject, input} from '@angular/core';
import {LogbookService} from '../../services/logbook.service';
import {NotificationService} from '../../shared/notification.service';
import {Utils} from '../../shared/utils';
import {LogService} from '../../shared/log.service';
import {QsoDetailModel} from '../../models/qso-detail.model';
import {DatePipe} from '@angular/common';
import {Fieldset} from 'primeng/fieldset';

@Component({
  selector: 'app-qso',
  templateUrl: './qso.component.html',
  styleUrl: './qso.component.scss',
  imports: [DatePipe, Fieldset],
})
export class QsoComponent {
  private _logbookSvc = inject(LogbookService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  logId = input<number>(0);
  qsoDetails: QsoDetailModel | undefined;

  constructor() {
    effect(() => {
      if (this.logId() > 0) // could also be -1
        this.loadQSO();
    });
  }

  private loadQSO() {
    this._logbookSvc.getQso(this.logId()!).subscribe({
      next: r => {
        this.qsoDetails = r;
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  qslStatus(status?: string, date?: Date|null, via?: string|null): string {
    if (status !== 'Y' && status !== 'V')
      return 'No';

    let res = Utils.dateToYmd(date);

    switch (via) {
      case 'B': return `${res} via bureau`;
      case 'D': return `${res} (direct)`;
      case 'E': return `${res} (electronic)`;
      case 'M': return `${res} via manager`;
    }
    return '';
  }
}
