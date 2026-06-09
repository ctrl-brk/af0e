import {
  Component,
  computed,
  DestroyRef,
  inject,
  model,
  OnInit,
  signal,
  viewChild,
  ViewEncapsulation
} from '@angular/core';
import {Title} from '@angular/platform-browser';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {activationSchema, PotaActivationModel} from '../../../models/pota-activation.model';
import {PotaService} from '../../../services/pota.service';
import {LogbookService} from '../../../services/logbook.service';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {ActivatedRoute, Router} from '@angular/router';
import {DatePipe} from '@angular/common';
import {Tag} from 'primeng/tag';
import {Card} from 'primeng/card';
import {Tab, TabList, TabPanel, TabPanels, Tabs} from 'primeng/tabs';
import {PotaActivationLogComponent} from './activation-log.component';
import {
  activationLogToAdif,
  ActivationQsoModel,
  QsoDetailsToActivationQsoModel
} from '../../../models/activation-qso.model';
import {PotaActivationMapComponent} from './activation-map.component';
import {Button} from 'primeng/button';
import {AppAuthService} from '../../../services/auth.service';
import {Dialog} from 'primeng/dialog';
import {QsoEditComponent, QsoEditParams} from '../../qso/qso-edit.component';
import {FloatLabelModule} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {InfraService} from '../../../services/infra.service';
import {PotaAppService} from '../../../services/pota-app.service';
import {PotaActivationInfoComponent} from './activation-info.component';
import {Tooltip} from 'primeng/tooltip';
import {form, FormField} from '@angular/forms/signals';
import {DatePicker} from 'primeng/datepicker';
import {NotificationMessageModel, NotificationMessageSeverity} from '../../../shared/notification-message.model';
import {QsoEditMode} from '../../../shared/qso-edit-mode.enum';
import {ConfirmPopup} from 'primeng/confirmpopup';
import {ConfirmationService} from 'primeng/api';
import {AdifImportResponseModel} from '../../../models/adif-import-response.model';
import {LogUpdatesService} from '../../../services/log-updates.service';
import {QsoDetailModel} from '../../../models/qso-detail.model';
import {ActivationStatusService} from '../../../services/activation-status.service';
import {defaultTitle} from '../../../shared/constants';

@Component({
  templateUrl: './activation.component.html',
  styleUrl: './activation.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    Card,
    FloatLabelModule,
    InputText,
    FormsModule,
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    PotaActivationLogComponent,
    Tag,
    DatePipe,
    PotaActivationMapComponent,
    Button,
    Dialog,
    QsoEditComponent,
    PotaActivationInfoComponent,
    Tooltip,
    FormField,
    DatePicker,
    ReactiveFormsModule,
    ConfirmPopup,
  ],
  providers: [
    ConfirmationService
  ]
})
export class PotaActivationComponent implements OnInit {
  private _titleSvc = inject(Title);
  private _router = inject(Router);
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _confirmSvc = inject(ConfirmationService);
  private _potaSvc = inject(PotaService);
  private _logbookSvc = inject(LogbookService);
  private _logUpdatesSvc = inject(LogUpdatesService);
  private _activationStatusSvc = inject(ActivationStatusService);
  private _infraSvc= inject(InfraService);
  private _potaAppSvc= inject(PotaAppService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _lastDupCheck = '';

  private activationInfo = viewChild(PotaActivationInfoComponent);

  protected _authSvc = inject(AppAuthService);
  protected activationId = signal(0);
  //protected logId = signal(-1);
  protected activation = signal<PotaActivationModel>(null!);
  protected logEntries = signal<ActivationQsoModel[]>([]);
  protected qsoDlgHeader = signal('');
  protected qsoEditVisible = model(false);
  protected spotDlgVisible = model(false);
  protected editActivationVisible = model(false);
  protected spotFreq = signal('14048.00');
  protected spotComment = signal('CQ');
  protected qsoStats = {total: 1, cw: 0, digi: 0, phone: 0};
  protected activationForm = form(this.activation, activationSchema)
  protected qsoEditMode = QsoEditMode.View;
  protected copyDlgVisible = signal(false);
  protected copyParkNum = signal('');

  protected qsoRate = computed(() => {
    const entries = this.logEntries();
    if (entries.length < 2) return null;
    const recent = [...entries]
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
      .slice(0, 10);
    if (recent.length < 2) return null;
    const newest = new Date(recent[0].date).getTime();
    const oldest = new Date(recent[recent.length - 1].date).getTime();
    const hours = (newest - oldest) / 3_600_000;
    if (hours === 0) return null;
    return Math.round(recent.length / hours);
  });

  protected lastQso = computed(() => {
    const entries = this.logEntries();
    if (!entries.length) return null;
    return [...entries].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())[0];
  });
  protected readonly qsoEditParams = signal<QsoEditParams>({});

  ngOnInit(): void {
    this._titleSvc.setTitle('AFØE - POTA | Activation');
    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {
        this.activationId.set(parseInt(x.get('id')!));
        this.onActivationChange(this.activationId());
      }
    });

    this._logUpdatesSvc.ensureConnected().catch(err => this._log.error(err));
    const updatesSub = this._logUpdatesSvc.changed$.subscribe(evt => {
      if (evt.operation === 'updated') //rare case, OK to reload the log (small most of the time)
        this.loadActivationLog(this.activationId());
      else if (evt.activationId === this.activationId() && evt.operation === 'created') //the updated event above doesn't have the activationId, so no check, and it's a rare case anyway
        this.loadNewQso(evt.logId!);
      //there's also 'imported', but we don't care here
    });

    this._destroyRef.onDestroy(() => {
      this._titleSvc.setTitle(defaultTitle);
      sub.unsubscribe();
      updatesSub.unsubscribe();
      this._activationStatusSvc.clear();
    });
  }

  private onActivationChange(id: number) {
    this._potaSvc.getActivation(id).subscribe({
      next: (r: PotaActivationModel) => {
        this.activation.set(r);
        this.qsoStats.total = r.count;
        this.qsoStats.cw = r.cwCount;
        this.qsoStats.digi = r.digiCount;
        this.qsoStats.phone = r.phoneCount;
        if (!r.endDate)
          this._activationStatusSvc.set(r.count);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this.loadActivationLog(id)
  }

  private loadActivationLog(activationId: number) {
    this._potaSvc.getActivationLog(activationId).subscribe({
      next: (r: ActivationQsoModel[]) => this.logEntries.set(r),
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  private loadNewQso(logId: number) {
    this._logbookSvc.getQso(logId).subscribe({
      next: (q: QsoDetailModel) => {
        const aqm = QsoDetailsToActivationQsoModel(q);
        this.logEntries.set([aqm, ...this.logEntries()]);

        if (q.mode === 'CW')
          this.qsoStats.cw++;
        else if (q.mode.startsWith('FT'))
          this.qsoStats.digi++;
        else
          this.qsoStats.phone++;

        this.qsoStats.total++;
        this._activationStatusSvc.set(this.logEntries().length);

        this.activationInfo()?.refresh();
      },
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });

  }

  onTabChange(tab: unknown) {

  }

  protected onAddQso() {
    this.qsoEditParams.set({potaActivation: this.activation()});
    this.qsoEditMode = QsoEditMode.PotaActivatingAdd
    this.qsoDlgHeader.set('Add QSO');
    this.qsoEditVisible.set(true);
  }

  protected onQsoSaved() {
    if (this.qsoEditParams().logId || 0 > 0)
      this.qsoEditVisible.set(false);
    //the qso gets added from signalr subscription
  }

  protected onQsoSelected(logId: number) {
    this.qsoEditParams.set({logId, potaActivation: this.activation()});
    this.qsoEditMode = QsoEditMode.Edit
    this.qsoDlgHeader.set('Edit QSO');
    this.qsoEditVisible.set(true);
  }

  protected onQsoDeleted() {
    this.loadActivationLog(this.activationId());
  }

  protected onSaveActivation() {
    const act = {
      ...this.activation(),
      startDate: Utils.dateToUtcString(this.activation().startDate),
      endDate: Utils.dateToUtcString(this.activation().endDate),
      logSubmittedDate: Utils.dateToUtcString(this.activation().logSubmittedDate),
      lat: parseFloat(this.activation().lat as any),
      long: parseFloat(this.activation().long as any),
    };

    this._potaSvc.updateActivation(act as any).subscribe({
      next: () => { this.editActivationVisible.set(false); },
      error: (e) => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected onNewActivationCreated(id: number) {
    this.activationId.set(id);
    this.onActivationChange(id);
    this.onAddQso();
  }

  protected onSpotShow() {
    this._infraSvc.getRigStatus().subscribe({
      next: (r) => {
        const f = r.frequencyHz.toString().slice(0, -1);
        let f2 = f.slice(-2);
        f2 = f2 === '00' ? '' : `.${f2}`;
        this.spotFreq.set(`${f.slice(0, -2)}${f2}`);
      },
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });

    this.spotDlgVisible.set(true);
  }

  protected onSpot() {
    this._potaAppSvc.addSpot(this.activation().stationCallsign, this.activation().parkNum, this.spotFreq(), Utils.frequencyToMode(this.spotFreq()), this.spotComment()).subscribe({
      next: () => this.spotDlgVisible.set(false),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected onQsoFormInit(qso: { call: string; qsoCount: number, DE: { grid: string; city: string; county: string; state: string } }) {
    if (this.qsoEditMode !== QsoEditMode.PotaActivatingAdd) return;

    this.qsoDlgHeader.set(`${qso.call.replace('0', 'Ø')} (${qso.qsoCount}) de ${qso.DE.grid} ${qso.DE.city}, ${qso.DE.county}, ${qso.DE.state}`);

    if (this._lastDupCheck === qso.call) return;

    this._lastDupCheck = qso.call;
    const existing = this.logEntries().filter(e => e.call === qso.call);
    if (existing.length === 0) return;

    Utils.showWarningMessage('DUP!', `${qso.call} ${existing.length} QSOs before`, this._ntfSvc);
  }

  protected setEndTimeAndStatus() {
    const endDate = this.logEntries().reduce((latest, entry) => {
      const entryDate = new Date(entry.date);
      return entryDate > latest ? entryDate : latest;
    }, new Date(0));

    this.activation().endDate = new Date(Math.ceil(endDate.getTime() / 60_000) * 60_000);
    this.activationForm.status().value.set('C');
  }

  protected onOpenGoogleMaps(): void {
    const lat = this.activationForm.lat().value();
    const lon = this.activationForm.long().value();
    if (!lat || !lon) return;
    window.open(`https://www.google.com/maps?q=${lat},${lon}`, '_blank');
  }

  protected onCopyActivation() {
    this._potaSvc.copyActivation(this.activationId(), this.copyParkNum()).subscribe({
      next: (id: number) => {
        this.copyDlgVisible.set(false);
        this.copyParkNum.set('');
        this._router.navigate(['/pota/activations', id]);
      },
      error: (e) => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected onCloneActivation() {
    this._potaSvc.cloneActivation(this.activationId()).subscribe({
      next: (id: number) => {
        this._router.navigate(['/pota/activations', id]);
      },
      error: (e) => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected onMergeAdifFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];

    this._logbookSvc.uploadAdif(this.activationId(), file).subscribe({
      next: (r: AdifImportResponseModel) => {
        const severity = r.skipped.length > 0 ? NotificationMessageSeverity.Warn : NotificationMessageSeverity.Success;
        let title = 'ADIF'
        if (r.skipped.length > 0) {
          title += ' (see console)'
          this._log.warn(`ADIF import result: Total:${r.received} Accepted:${r.accepted} Skipped:${r.skipped} Qrz:${r.qrz}`);
        }
        this._ntfSvc.addMessage(new NotificationMessageModel(severity, title, `Total:${r.received} Accepted:${r.accepted} Skipped:${r.skipped.length} Qrz:${r.qrz}`, true));
        this.onActivationChange(this.activationId());
      },
      error: (e) => {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    });
    input.value = '';
  }

  protected onConfirmDelete(event: Event) {
    this._confirmSvc.confirm({
      target: event.currentTarget as EventTarget,
      message: 'Delete the activation?',
      icon: 'pi pi-info-circle',
      rejectButtonProps: {
        label: 'Cancel',
        severity: 'secondary',
        outlined: true
      },
      acceptButtonProps: {
        label: 'Delete',
        severity: 'danger'
      },
      accept: () => {
        this._potaSvc.deleteActivation(this.activationId()).subscribe({
          next: () => { this._router.navigate(['/pota/activations']); },
          error: (e) => Utils.showErrorMessage(e, this._ntfSvc, this._log)
        });
      },
      reject: () => {}
    });
  }

  protected onExportAdif() {
    const activation = this.activation();
    const entries = this.logEntries();
    if (!entries.length || !activation) return;

    const { content, filename } = activationLogToAdif(entries, activation);
    const blob = new Blob([content], {type: 'application/octet-stream'});
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
