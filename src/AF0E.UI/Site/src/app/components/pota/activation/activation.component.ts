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
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {activationSchema, PotaActivationModel} from '../../../models/pota-activation.model';
import {PotaService} from '../../../services/pota.service';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {ActivatedRoute} from '@angular/router';
import {DatePipe} from '@angular/common';
import {Tag} from 'primeng/tag';
import {Card} from 'primeng/card';
import {Tab, TabList, TabPanel, TabPanels, Tabs} from 'primeng/tabs';
import {PotaActivationLogComponent} from './activation-log.component';
import {ActivationQsoModel} from '../../../models/activation-qso.model';
import {PotaActivationMapComponent} from './activation-map.component';
import {Button} from 'primeng/button';
import {AppAuthService} from '../../../services/auth.service';
import {Dialog} from 'primeng/dialog';
import {QsoEditComponent} from '../../logbook/qso-edit.component';
import {Checkbox} from 'primeng/checkbox';
import {FloatLabelModule} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {InfraService} from '../../../services/infra.service';
import {PotaAppService} from '../../../services/pota-app.service';
import {PotaActivationInfoComponent} from './activation-info.component';
import {Tooltip} from 'primeng/tooltip';
import {form, FormField} from '@angular/forms/signals';
import {DatePicker} from 'primeng/datepicker';

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
    Checkbox,
    PotaActivationInfoComponent,
    Tooltip,
    FormField,
    DatePicker,
    ReactiveFormsModule,
  ],
})
export class PotaActivationComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _potaSvc = inject(PotaService);
  private _infraSvc= inject(InfraService);
  private _potaAppSvc= inject(PotaAppService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  private activationInfo = viewChild(PotaActivationInfoComponent);

  protected _authSvc = inject(AppAuthService);
  protected activationId = signal(0);
  protected logId = signal(-1);
  protected activation = signal<PotaActivationModel>(null!);
  protected logEntries = signal<ActivationQsoModel[]>([]);
  protected qsoEditVisible = model(false);
  protected spotDlgVisible = model(false);
  protected editActivationVisible = model(false);
  protected rigControl = signal(false);
  protected keyerControl = signal(false);
  protected rigCommanderConfig = signal<any>({});
  protected spotFreq = signal('14048.00');
  protected spotComment = signal('CQ');
  protected qsoStats = {total: 1, cw: 0, digi: 0, phone: 0};
  protected activationForm = form(this.activation, activationSchema)

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

  ngOnInit(): void {
    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {
        this.activationId.set(parseInt(x.get('id')!));
        this.onActivationChange(this.activationId());
      }
    });

    this._infraSvc.getConfig().subscribe({
      next: (r: any) => {
        this.rigCommanderConfig.set(r);
        this.rigControl.set(true);
      },
      error: () => {
        this.rigControl.set(false);
      }
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
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
          setTimeout(() => this.activationInfo()?.refresh(), 100);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this.loadActivationLog(id)
  }

  private loadActivationLog(id: number) {
    this._potaSvc.getActivationLog(id).subscribe({
      next: (r: ActivationQsoModel[]) => this.logEntries.set(r),
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  onTabChange(tab: unknown) {

  }

  protected onAddQso() {
    this.logId.set(-1);
    this.qsoEditVisible.set(true);
  }

  protected onQsoSaved(qso: any) {
    if (this.logId() > 0)
      this.qsoEditVisible.set(false);

    this.loadActivationLog(this.activationId());

    if (qso.mode === 'CW')
      this.qsoStats.cw++;
    else
      this.qsoStats.phone++;

    this.qsoStats.total++;

    this.activationInfo()?.refresh();
  }

  protected onQsoSelected(logId: number) {
    this.logId.set(logId);
    this.qsoEditVisible.set(true);
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
    if (this.rigControl()) {

      this._infraSvc.getRigStatus().subscribe({
        next: (r) => {
          const f = r.frequencyHz.toString().slice(0, -1);
          let f2 = f.slice(-2);
          f2 = f2 === '00' ? '' : `.${f2}`;
          this.spotFreq.set(`${f.slice(0, -2)}${f2}`);
        },
        error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
      });
    }

    this.spotDlgVisible.set(true);
  }

  protected onSpot() {
    this._potaAppSvc.addSpot(this.activation().parkNum, this.spotFreq(), this.spotComment()).subscribe({
      next: () => this.spotDlgVisible.set(false),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
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

  protected onExportAdif() {
    const activation = this.activation();
    const entries = this.logEntries();
    if (!entries.length || !activation) return;

    const field = (name: string, value: string | number | null | undefined): string => {
      if (!value) return '';
      if (typeof value === 'number') value = value.toString();
      return `<${name}:${value.length}>${value}`;
    };

    const mapMode = (mode: string | null): string | null => {
      if (!mode) return mode;
      const m = mode.toUpperCase();
      if (m === 'USB' || m === 'LSB') return 'SSB';
      if (m === 'MFSK') return 'FT4';
      return m;
    };

    const header =
      `ADIF for AF0E: POTA at ${activation.parkNum} ${activation.parkName} on ${activation.startDate}\n` +
      `<ADIF_VER:5>3.1.5\n` +
      `<PROGRAMID:8>AF0E.org\n` +
      `<EOH>\n`;

    const records = entries.map(qso => {
      const d = new Date(qso.date);
      const qsoDate =
        `${d.getFullYear()}` +
        `${String(d.getMonth() + 1).padStart(2, '0')}` +
        `${String(d.getDate()).padStart(2, '0')}`;
      const timeOn =
        `${String(d.getHours()).padStart(2, '0')}` +
        `${String(d.getMinutes()).padStart(2, '0')}` +
        `${String(d.getSeconds()).padStart(2, '0')}`;

      let rec = '';
      rec += field('BAND', qso.band);
      rec += field('CALL', qso.call);
      rec += field('CQZ', qso.cqz);
      rec += field('DXCC', qso.dxcc);
      rec += field('FREQ', qso.freq ? (qso.freq / 1_000_000).toFixed(6) : null);
      rec += field('GRIDSQUARE', qso.grid);
      rec += field('ITUZ', qso.ituz);
      rec += field('MODE', mapMode(qso.mode));
      rec += field('MY_CITY', qso.myCity);
      if (activation.county) rec += field('MY_CNTY', qso.myCnty);
      rec += field('MY_COUNTRY', qso.myCountry);
      if (activation.grid) rec += field('MY_GRIDSQUARE', qso.myGrid);
      rec += field('MY_POTA_REF', activation.parkNum);
      rec += field('MY_SIG', 'POTA');
      rec += field('MY_SIG_INFO', activation.parkNum);
      if (activation.state) rec += field('MY_STATE', qso.myState);
      rec += field('OPERATOR', 'AF0E');
      rec += field('QSLMSG', `POTA ${activation.parkNum}`);
      rec += field('QSO_DATE', qsoDate);
      rec += field('RST_RCVD', qso.rstRcvd);
      rec += field('RST_SENT', qso.rstSent);
      rec += field('STATE', qso.state);
      rec += field('STATION_CALLSIGN', 'AF0E');
      rec += field('TIME_ON', timeOn);
      /* if (qso.p2p?.length) {
        const p2pStr = qso.p2p.join(',');
        rec += field('SIG', 'POTA');
        rec += field('SIG_INFO', p2pStr);
        rec += field('POTA_REF', p2pStr);
      }
      if (qso.satName) {
        rec += field('SAT_NAME', qso.satName);
        rec += field('PROP_MODE', 'SAT');
      } */
      rec += '<EOR>\n';
      return rec;
    }).join('');

    const blob = new Blob([header + records], {type: 'application/octet-stream'});
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const sd = activation.startDate;
    const dateStr = `${sd.getFullYear()}-${String(sd.getMonth() + 1).padStart(2, '0')}-${String(sd.getDate()).padStart(2, '0')}`;
    a.download = `${dateStr} ${activation.parkNum} (${Utils.abbreviateParkName(activation.parkName)}).adi`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
