import {Component, DestroyRef, inject, model, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {TableLazyLoadEvent, TableModule} from 'primeng/table';
import {LogbookService} from '../../services/logbook.service';
import {Utils} from '../../shared/utils';
import {QsoSummaryModel} from '../../models/qso-summary.model';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {TagModule} from 'primeng/tag';
import {FormsModule} from '@angular/forms';
import {FloatLabelModule} from 'primeng/floatlabel';
import {NotificationMessageModel, NotificationMessageSeverity} from '../../shared/notification-message.model';
import {SortDirection} from '../../shared/sort-direction.enum';
import {DatePickerModule} from 'primeng/datepicker';
import {DialogModule} from 'primeng/dialog';
import {QsoComponent} from '../qso/qso.component';
import {TooltipModule} from 'primeng/tooltip';
import {Button} from 'primeng/button';
import {ScrollTop} from 'primeng/scrolltop';
import {ModeSeverityPipe, QsoModePipe} from '../../shared/pipes';
import {DatePipe} from '@angular/common';
import {AppAuthService} from '../../services/auth.service';
import {QsoEditComponent} from './qso-edit.component';

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
    QsoComponent,
    ScrollTop,
    TableModule,
    TagModule,
    TooltipModule,
    ModeSeverityPipe,
    QsoModePipe,
    QsoEditComponent,
  ],
})
export class LogbookComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _router = inject(Router);
  protected _authSvc = inject(AppAuthService);
  private _logbookSvc = inject(LogbookService);
  private _ntfSvc = inject(NotificationService);
  private _log = inject(LogService);

  private _call: string | null = null;

  // Convert to signals for better zoneless change detection
  logEntries = signal<QsoSummaryModel[]>([]);
  totalRecords = signal(0);
  selectedId = signal(0);
  loading = signal(false);
  qsoDateRange = model<Date[]>([]); // model() for two-way binding
  qsoMinDate = signal<Date | undefined | null>(undefined);
  qsoMaxDate = signal<Date | undefined | null>(undefined);
  dateRangeTitle = signal('');
  qsoDetailsVisible = model(false); // model() for two-way binding with dialog
  qsoEditVisible = model(false); // model() for two-way binding with dialog
  myCallsign = signal('');

  ngOnInit() {

    const now = new Date();
    this.qsoMinDate.set(new Date(2009, 4));
    this.qsoMaxDate.set(new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
    this.qsoDateRange.set([this.qsoMinDate()!, this.qsoMaxDate()!]);
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
    this.loadLog(call, 0, 50, [this.qsoMinDate()!, this.qsoMaxDate()!]);
  }

  onLazyLoad(event: TableLazyLoadEvent) {
    this.loadLog(this._call, event.first!, event.rows!, this.qsoDateRange(), event.sortOrder === 1 ? SortDirection.Ascending : SortDirection.Descending);
  }

  onDateRangeFocus(focus: boolean) {
    this.dateRangeTitle.set(focus ? 'yyyy-mm-dd - yyyy-mm-dd' : 'Date range...');
  }

  onDateRangeSearch() {
    if (!this.qsoDateRange() || !this.qsoDateRange()[0] || !this.qsoDateRange()[1])
      this._ntfSvc.addMessage(new NotificationMessageModel(NotificationMessageSeverity.Warn, 'Please select a date range'));

    this.loadLog(this._call, 0, 50, this.qsoDateRange());
  }

  private loadLog(call: string | null, skip: number, take: number = 0, dateRange?: Date[], order: SortDirection = SortDirection.Descending, sortBy = 'date') {
    this.loading.set(true);

    let minDate = !dateRange || !dateRange[0] || !dateRange[1] ? this.qsoMinDate() : dateRange[0];
    let maxDate = !dateRange || !dateRange[0] || !dateRange[1] ? this.qsoMaxDate() : dateRange[1];

    this._logbookSvc.getQsoSummaries(call, skip, take, order, sortBy, [minDate!, maxDate!]).subscribe({
      next: r => {
        this.totalRecords.set(r.totalCount);
        this.logEntries.set(r.contacts);
        this.loading.set(false);
        // No ChangeDetectorRef needed! Signals automatically notify Angular
      },
      error: e => {
        this.loading.set(false);
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    });
  }

  protected onAddQso() {
    this.selectedId.set(this.selectedId() === 0 ? -1 : 0); // triggers form init
    this.qsoEditVisible.set(true);
  }

  onQsoSelect(qso: QsoSummaryModel) {
    this.selectedId.set(qso.id);

    if (this._authSvc.hasRole('Admin')) {
      this.qsoEditVisible.set(true);
      return;
    }

    if (qso.date > new Date(Date.UTC(2011, 0, 6)))
      this.myCallsign.set('AFØE');
    else if (qso.date > new Date(2010, 10, 21))
      this.myCallsign.set('K3OSO');
    else
      this.myCallsign.set('KDØHHE');

    this.qsoDetailsVisible.set(true);
  }

  onQsoSaved(isUpdate: boolean) {
    this.qsoEditVisible.set(false);

    if (!isUpdate) {
      this.loadLog(this._call, 0, 50, this.qsoDateRange());
    }
  }
}
