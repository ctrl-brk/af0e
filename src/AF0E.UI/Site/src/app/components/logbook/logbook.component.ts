import {Component, DestroyRef, inject, OnInit, ViewEncapsulation} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {TableLazyLoadEvent, TableModule} from 'primeng/table';
import {LogbookService} from '../../services/logbook.service';
import {Utils} from '../../shared/utils';
import {QsoSummaryModel} from '../../models/qso-summary.model';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {DatePipe} from '@angular/common';
import {TagModule} from 'primeng/tag';
import {FormsModule} from '@angular/forms';
import {FloatLabelModule} from 'primeng/floatlabel';
import {NotificationMessageModel, NotificationMessageSeverity} from '../../shared/notification-message.model';
import {SortDirection} from '../../shared/sort-direction.enum';
import {DatePickerModule} from 'primeng/datepicker';
import {DialogModule} from 'primeng/dialog';
import {QsoComponent} from '../qso/qso.component';
import {TooltipModule} from 'primeng/tooltip';
import {MatIcon, MatIconRegistry} from '@angular/material/icon';
import {Button} from 'primeng/button';
import {ScrollTop} from 'primeng/scrolltop';
import {ModeSeverityPipe, QsoModePipe} from '../../shared/pipes';

@Component({
  selector: 'app-logbook',
  templateUrl: './logbook.component.html',
  styleUrl: './logbook.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    Button,
    DatePickerModule,
    DatePipe,
    DialogModule,
    FloatLabelModule,
    FormsModule,
    MatIcon,
    QsoComponent,
    ScrollTop,
    TableModule,
    TagModule,
    TooltipModule,
    ModeSeverityPipe,
    QsoModePipe,
  ],
})
export class LogbookComponent implements OnInit{
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _matIconReg = inject(MatIconRegistry);
  private _logbookSvc = inject(LogbookService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _call: string | null = null;

  logEntries: QsoSummaryModel[] = [];
  totalRecords = 0;
  selectedId?: number;
  loading = false;
  qsoDateRange!: Date[];
  qsoMinDate: Date | undefined | null;
  qsoMaxDate: Date | undefined | null;
  dateRangeTitle = '';
  qsoDetailsVisible = false;
  myCallsign = '';

  ngOnInit() {
    this._matIconReg.setDefaultFontSetClass('material-symbols-outlined');

    const now = new Date();
    this.qsoMinDate = new Date(2009, 4);
    this.qsoMaxDate = new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
    this.qsoDateRange = [this.qsoMinDate, this.qsoMaxDate];
    this.onDateRangeFocus(false);

    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {
        let prefix = x.get('prefix') ? x.get('prefix') + '/' : '';
        let call = x.get('call');
        let suffix = x.get('suffix') ? '/' + x.get('suffix') : '';
        this._call = call ? `${prefix}${call}${suffix}` : null;
        this.onCallChange(this._call);
      }
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private onCallChange(call: string | null) {
    this.loadLog(call, 0, 50, [this.qsoMinDate!, this.qsoMaxDate!]);
  }

  onLazyLoad(event: TableLazyLoadEvent) {
    this.loadLog(this._call, event.first!, event.rows!, this.qsoDateRange, event.sortOrder === 1 ? SortDirection.Ascending : SortDirection.Descending);
  }

  onDateRangeFocus(focus: boolean) {
    this.dateRangeTitle = focus ? 'yyyy-mm-dd - yyyy-mm-dd' : 'Date range...';
  }

  onDateRangeSearch() {
    if (!this.qsoDateRange || !this.qsoDateRange[0] || !this.qsoDateRange[1])
      this._ntfSvc.addMessage(new NotificationMessageModel(NotificationMessageSeverity.Warn, 'Please select a date range'));

    this.loadLog(this._call, 0, 50, this.qsoDateRange);
  }

  private loadLog(call: string | null, skip: number, take: number = 0, dateRange?: Date[], order: SortDirection = SortDirection.Descending, sortBy = 'date') {
    this.loading = true;

    let minDate = !dateRange || !dateRange[0] || !dateRange[1] ? this.qsoMinDate : dateRange[0];
    let maxDate = !dateRange || !dateRange[0] || !dateRange[1] ? this.qsoMaxDate : dateRange[1];

    this._logbookSvc.getQsoSummaries(call, skip, take, order, sortBy, [minDate!, maxDate!]).subscribe({
      next: r => {
        this.totalRecords = r.length > 0 ? r[0].totalCount : 0;
        this.logEntries = r;
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
      complete: () => this.loading = false
    });
  }

  onQsoSelect(qso: QsoSummaryModel) {
    if (qso.date > new Date(Date.UTC(2011, 0, 6)))
      this.myCallsign = 'AFØE';
    else if (qso.date > new Date(2010, 10, 21))
      this.myCallsign = 'K3OSO';
    else
      this.myCallsign = 'KDØHHE';

    this.selectedId = qso.id;
    this.qsoDetailsVisible = true;
  }
}
