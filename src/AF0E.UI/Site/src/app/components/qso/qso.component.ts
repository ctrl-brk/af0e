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
  standalone: true,
  imports: [
    DatePipe,
    Fieldset
  ],
  templateUrl: './qso.component.html',
  styleUrl: './qso.component.scss'
})
export class QsoComponent {
  private _logbookSvc = inject(LogbookService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  qsoId = input<number>();
  qsoDetails: QsoDetailModel | undefined;

  constructor() {
    effect(() => {
      if (this.qsoId())
        this.loadQSO();
    });
  }

  private loadQSO() {
    this._logbookSvc.GetQso(this.qsoId()!).subscribe({
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
