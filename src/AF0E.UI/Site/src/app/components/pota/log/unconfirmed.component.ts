import {Component, DestroyRef, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {TableModule} from 'primeng/table';
import {PotaService} from '../../../services/pota.service';
import {QsoSummaryModel} from '../../../models/qso-summary.model';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {Card} from 'primeng/card';
import {DatePipe} from '@angular/common';
import {ModeSeverityPipe, QsoModePipe} from '../../../shared/pipes';
import {Tag} from 'primeng/tag';
import {HttpClient} from '@angular/common/http';
import {defaultTitle} from '../../../shared/constants';

@Component({
  templateUrl: './unconfirmed.component.html',
  styleUrl: './unconfirmed.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    TableModule,
    Card,
    DatePipe,
    ModeSeverityPipe,
    QsoModePipe,
    Tag,
  ],
})
export class PotaUnconfirmedComponent implements OnInit {
  private _titleSvc = inject(Title);
  private _destroyRef = inject(DestroyRef);
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  private http = inject(HttpClient);

  protected logEntries = signal<QsoSummaryModel[]>([]);

  ngOnInit(): void {
    this._titleSvc.setTitle('AFØE - POTA | Unconfirmed');

      this._potaSvc.getUnconfirmedLog().subscribe({
      next: (r: QsoSummaryModel[]) => {
        this.logEntries.set(r);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this._destroyRef.onDestroy(() => {
      this._titleSvc.setTitle(defaultTitle);
    });
  }
}
