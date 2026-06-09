import {
  Component,
  computed,
  DestroyRef,
  effect,
  inject,
  model,
  OnDestroy,
  OnInit,
  signal,
  ViewEncapsulation
} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {Title} from '@angular/platform-browser';
import {Utils} from '../../../shared/utils';
import {PotaService} from '../../../services/pota.service';
import {InfraService} from '../../../services/infra.service';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {PotaActivityStatsModel} from '../../../models/pota-activity-stats.model';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {ModeSeverityPipe, QsoModePipe, TimeAgoPipe} from '../../../shared/pipes';
import {Button} from 'primeng/button';
import {Checkbox} from 'primeng/checkbox';
import {FormsModule} from '@angular/forms';
import {DatePipe, NgClass} from '@angular/common';
import {Dialog} from 'primeng/dialog';
import {QsoEditComponent, QsoEditParams} from '../../qso/qso-edit.component';
import {ParkHuntingStatsComponent} from '../park/stats/park-hunting-stats.component';
import {Badge} from 'primeng/badge';
import {PotaAppService} from '../../../services/pota-app.service';
import {QsoEditMode} from '../../../shared/qso-edit-mode.enum';
import {defaultTitle} from '../../../shared/constants';

@Component({
  templateUrl: './pota-spots.component.html',
  styleUrl: './pota-spots.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    TableModule,
    Tag,
    QsoModePipe,
    ModeSeverityPipe,
    TimeAgoPipe,
    Button,
    Checkbox,
    FormsModule,
    NgClass,
    Dialog,
    QsoEditComponent,
    ParkHuntingStatsComponent,
    Badge,
    DatePipe,
  ]
})
export class PotaSpotsComponent implements OnInit, OnDestroy {
  private _titleSvc = inject(Title);
  private _destroyRef = inject(DestroyRef);
  private _activatedRoute = inject(ActivatedRoute);
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _infraSvc= inject(InfraService);
  private _potaAppSvc= inject(PotaAppService);
  private _log = inject(LogService);

  private _allSpots = signal<PotaActivityStatsModel[]>([]);
  // Hash table of clicked callsigns for a quick lookup in the grid.
  private _clickedCallSigns = signal<Record<string, true>>({});
  private _spotFreq = 0;
  private _spotParkNum = '';
  private _huntingActivationId: number|undefined = undefined;
  private _huntingStationCall: string|undefined = undefined;

  protected spots = computed(() => {
    const all = this._allSpots();
    const showDigitalModes = this.showDigi();
    const showPhoneModes = this.showPhone();
    const showCwMode = this.showCw();
    const beam = this.beam();
    const slopper = this.slopper();

    if (showDigitalModes && showPhoneModes && showCwMode)
      return all;

    let ant = all;
    if (!beam || !slopper) {
      // Filter antennas
      ant = all.filter(spot => {
        if (!slopper && (spot.activity.band === '40m' || spot.activity.band === '80m' || spot.activity.band === '160m'))
          return false;
        return !(!beam && (spot.activity.band !== '40m' && spot.activity.band !== '80m' && spot.activity.band !== '160m'));
      });
    }

    // Filter modes
    return ant.filter(spot => {
      const mode = spot.activity.mode ? spot.activity.mode.toUpperCase() : '';

      if (![showDigitalModes, showPhoneModes, showCwMode].includes(true)) {
        return ![mode.startsWith('FT'), mode === 'RTTY', mode === '', mode.endsWith('SB'), mode==='CW'].includes(true);
      }

      return (showDigitalModes && mode.startsWith('FT') || mode === 'RTTY') ||
        (showPhoneModes && (mode === '' || mode.endsWith('SB'))) ||
        (showCwMode && mode === 'CW');
    });
  });

  protected readonly QsoEditMode = QsoEditMode; //so we can use the enum value in html
  protected qsoDlgHeader = signal('');
  protected autoRefresh = signal(false);
  protected showDigi = signal(false);
  protected showPhone = signal(true);
  protected showCw = signal(true);
  protected beam = signal(true);
  protected slopper = signal(false);
  protected showDups = signal(false);
  protected selectedParkNum = signal('');
  private refreshInterval?: ReturnType<typeof setInterval>;
  protected qsoEditVisible = model(false); // model() for two-way binding with dialog
  protected huntingStatsVisible = model(false);
  protected isRefreshing = signal(false);
  protected lastQso = signal<any>(null);
  protected readonly qsoEditParams = signal<QsoEditParams>({});

  constructor() {
    effect(() => {
      if (this.autoRefresh()) {
        this.refreshInterval = setInterval(() => {
          this.refreshSpots();
        }, 30000);
      } else {
        if (this.refreshInterval) {
          clearInterval(this.refreshInterval);
          this.refreshInterval = undefined;
        }
      }
    });
  }

  ngOnInit(): void {
    this._titleSvc.setTitle('AFØE - POTA | Spots');

    const activationId = this._activatedRoute.snapshot.queryParamMap.get('activationId');
    if (activationId) this._huntingActivationId = parseInt(activationId);

    const stationCall = this._activatedRoute.snapshot.queryParamMap.get('stationCall');
    if (stationCall) this._huntingStationCall = stationCall;

    this.refreshSpots();

    this._destroyRef.onDestroy(() => {
      this._titleSvc.setTitle(defaultTitle);
    });
  }

  protected onDupsChange(checked: boolean) {
    this.showDups.set(checked);
    this.refreshSpots();
  }

  protected refreshSpots() {
    this.isRefreshing.set(true);
    this._potaSvc.getActivity( this._huntingActivationId, undefined, undefined, this.showDups()).subscribe({
      next: (r: PotaActivityStatsModel[]) => {
        this._allSpots.set(r);
        this.isRefreshing.set(false);
      },
      error: (e)=> {
        this.isRefreshing.set(false);
        Utils.showErrorMessage(e, this._ntfSvc, this._log)
      },
    });
  }

  protected onCallSignClick(callSign: string, parkNum: string, freqKhz: string, mode: string): void {
    this._spotParkNum = parkNum;
    this._spotFreq = Number(freqKhz);

    if (mode === 'SSB')
      mode = this._spotFreq > 14000 ? 'USB' : 'LSB';

    this._infraSvc.setRigStatus(this._spotFreq * 1000, mode).subscribe({
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });

    this.rememberClickedCallSign(callSign);
    this.qsoEditParams.set({callSign, huntingFromActivationId: this._huntingActivationId, huntingStationCall: this._huntingStationCall});
    this.qsoEditVisible.set(true);
  }

  protected wasCallSignClicked(callSign: string): boolean {
    const key = this.normalizeCallSign(callSign);
    return !!key && this._clickedCallSigns()[key];
  }

  private rememberClickedCallSign(callSign: string): void {
    const key = this.normalizeCallSign(callSign);
    if (!key) return;

    this._clickedCallSigns.update(map => ({ ...map, [key]: true }));
  }

  private normalizeCallSign(callSign: string): string {
    return (callSign || '').trim().toUpperCase();
  }

  protected onAddQso() {
    this.qsoEditParams.set({huntingFromActivationId: this._huntingActivationId, huntingStationCall: this._huntingStationCall});
    this.qsoEditVisible.set(true);
  }

  protected onParkClick(parkNum: string): void {
    this.selectedParkNum.set(parkNum);
    this.huntingStatsVisible.set(true);
  }

  protected onQsoSaved(qso: any) {
    this.lastQso.set(qso);
    this.addPotaSpot(qso);
    this.qsoEditVisible.set(false);
    this.refreshSpots();
  }

  private addPotaSpot(qso: any) {
    if (!this._spotParkNum) return;

    this._potaAppSvc.addSpot(qso.call, this._spotParkNum, this._spotFreq.toString(), qso.mode, `${qso.rstSent} in ${qso.myGrid.slice(0, 4)} CO. TU ${Utils.extractNameOrNickname(qso.name_fmt)}!`).subscribe({
      next: () => {},
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected onQsoFormInit(qso: { call: string; qsoCount: number, DE: { grid: string; city: string; county: string; state: string } }) {
    this.qsoDlgHeader.set(`${qso.call.replace('0', 'Ø')} (${qso.qsoCount}) de ${qso.DE.grid} ${qso.DE.city}, ${qso.DE.county}, ${qso.DE.state}`);
  }

  protected getRowClass(spot: PotaActivityStatsModel): string {
    const currentBand = spot.activity.band;
    const currentMode = spot.activity.mode;

    // Check if we have any contacts with this park
    if (spot.totalParkContacts === 0) {
      return 'wrk-no';
    }

    // Check if we have contact on the current band and mode
    const bandModeContact = spot.parkContactsByBandMode?.find(
      bc => bc.band === currentBand && bc.mode === currentMode
    );

    if (bandModeContact && bandModeContact.count > 0) {
      return 'wrk-bm';
    }

    // We have contacts but not on this band/mode
    return 'wrk-other';
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }
}
