import {Component, DestroyRef, inject, model, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {ActivatedRoute} from '@angular/router';
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
import {ModeSeverityPipe, QsoModePipe} from '../../shared/pipes';
import {DatePipe} from '@angular/common';
import {AppAuthService} from '../../services/auth.service';
import {QsoEditComponent} from './qso-edit.component';
import {QsoEditMode} from '../../shared/qso-edit-mode.enum';
import {LogUpdatesService} from '../../services/log-updates.service';
import {defaultTitle} from '../../shared/constants';
import {ContextMenu} from 'primeng/contextmenu';
import {MenuItem} from 'primeng/api';

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
    TableModule,
    TagModule,
    TooltipModule,
    ModeSeverityPipe,
    QsoModePipe,
    QsoEditComponent,
    ContextMenu,
  ],
})
export class LogbookComponent implements OnInit {
  private _titleSvc = inject(Title);
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  protected _authSvc = inject(AppAuthService);
  private _lbSvc = inject(LogbookService);
  private _logUpdatesSvc = inject(LogUpdatesService);
  private _ntfSvc = inject(NotificationService);
  private _log = inject(LogService);

  private _call: string | null = null;
  private gridState = {skip: 0, take: 50, sortDirection: SortDirection.Descending, sortBy: 'date'};

  protected cmItems: MenuItem[] = [];
  protected selectedQso!: QsoSummaryModel;
  protected logEntries = signal<QsoSummaryModel[]>([]);
  protected totalRecords = signal(0);
  selectedId = signal(0);
  loading = signal(false);
  qsoDateRange = model<Date[]>([]); // model() for two-way binding
  qsoMinDate = signal<Date | undefined | null>(undefined);
  qsoMaxDate = signal<Date | undefined | null>(undefined);
  dateRangeTitle = signal('');
  qsoDetailsVisible = model(false); // model() for two-way binding with dialog
  qsoEditVisible = model(false); // model() for two-way binding with dialog
  myCallsign = signal('');
  protected qsoEditMode = QsoEditMode.Add;

  ngOnInit() {
    this._titleSvc.setTitle('AFØE - Logbook');

    const adminSub = this._authSvc.hasRoleAsync('Admin').subscribe(isAdmin => {
      this.cmItems = isAdmin ? [{
        label: 'Delete', icon: 'pi pi-trash', command: () => { this.onDeleteQso(this.selectedQso); }
      }] : [];
    });
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

    this._logUpdatesSvc.ensureConnected().catch(err => this._log.error('Logbook realtime connection failed', err));
    const updatesSub = this._logUpdatesSvc.changed$.subscribe(evt => {
      if (evt.operation !== 'created' && evt.operation !== 'updated' && evt.operation !== 'imported')
        return;

      if (this._call && evt.call && this._call.toUpperCase() !== evt.call.toUpperCase())
        return;

      this.reloadLog();
    });

    this._destroyRef.onDestroy(() => {
      this._titleSvc.setTitle(defaultTitle);
      sub.unsubscribe();
      updatesSub.unsubscribe();
      adminSub.unsubscribe();
    });
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

  private reloadLog() {
    this.loadLog(this._call, this.gridState.skip, this.gridState.take, this.qsoDateRange(), this.gridState.sortDirection, this.gridState.sortBy);
  }

  private loadLog(call: string | null, skip: number, take: number = 0, dateRange?: Date[], order: SortDirection = SortDirection.Descending, sortBy = 'date') {
    this.loading.set(true);
    this.gridState = {skip, take, sortDirection: order, sortBy};

    let minDate = !dateRange || !dateRange[0] || !dateRange[1] ? this.qsoMinDate() : dateRange[0];
    let maxDate = !dateRange || !dateRange[0] || !dateRange[1] ? this.qsoMaxDate() : dateRange[1];

    this._lbSvc.getQsoSummaries(call, skip, take, order, sortBy, [minDate!, maxDate!]).subscribe({
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
    this.qsoEditMode = QsoEditMode.Add;
    this.selectedId.set(this.selectedId() === 0 ? -1 : 0); // triggers form init
    this.qsoEditVisible.set(true);
  }

  onQsoSelect(qso: QsoSummaryModel) {
    this.selectedId.set(qso.id);
    this.qsoEditMode = QsoEditMode.Edit;

    if (this._authSvc.isAdmin) {
      this.qsoEditVisible.set(true);
      return;
    }

    let call = qso.operatorCallsign ?? Utils.getMyEffectiveCall(qso.date);
    if (qso.stationCallsign && qso.stationCallsign !== call)
      call = `${call} @ ${qso.stationCallsign}`;

    this.myCallsign.set(call);

    this.qsoDetailsVisible.set(true);
  }

  onQsoSaved(qso: any) {
    this.qsoEditVisible.set(false);

    // if (!qso.id) {
    //   this.loadLog(this._call, 0, 50, this.qsoDateRange());
    // }
  }

  private onDeleteQso(qso: QsoSummaryModel) {
    this._lbSvc.deleteQso(qso.id).subscribe({
      next: () => this.reloadLog(),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected readonly QsoEditMode = QsoEditMode;
}
