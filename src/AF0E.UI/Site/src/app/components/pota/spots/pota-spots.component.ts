import {Component, computed, effect, inject, model, OnDestroy, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {Utils} from '../../../shared/utils';
import {PotaService} from '../../../services/pota.service';
import {InfraService} from '../../../services/infra.service';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {PotaActivityStatsModel} from '../../../models/pota-activity-stats.model';
import {ScrollTop} from 'primeng/scrolltop';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {ModeSeverityPipe, QsoModePipe, TimeAgoPipe} from '../../../shared/pipes';
import {Button} from 'primeng/button';
import {Checkbox} from 'primeng/checkbox';
import {FormsModule} from '@angular/forms';
import {NgClass} from '@angular/common';
import {Dialog} from 'primeng/dialog';
import {QsoEditComponent} from '../../logbook/qso-edit.component';
import {ParkHuntingStatsComponent} from '../park/stats/park-hunting-stats.component';
import {Badge} from 'primeng/badge';

@Component({
  templateUrl: './pota-spots.component.html',
  styleUrl: './pota-spots.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    ScrollTop,
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
  ]
})
export class PotaSpotsComponent implements OnInit, OnDestroy {
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _infraSvc= inject(InfraService);
  private _log = inject(LogService);
  private allSpots = signal<PotaActivityStatsModel[]>([]);

  protected spots = computed(() => {
    const all = this.allSpots();
    const showDigitalModes = this.showDigi();
    const showPhoneModes = this.showPhone();
    const showCwMode = this.showCw();

    if (showDigitalModes && showPhoneModes && showCwMode)
      return all;

    // Filter out modes
    return all.filter(spot => {
      const mode = spot.activity.mode ? spot.activity.mode.toUpperCase() : '';

      if (!showDigitalModes && mode.startsWith('FT'))
        return false;
      if (!showPhoneModes && mode.startsWith('SSB'))
        return false;

      return !(!showCwMode && mode === 'CW');
    });
  });

  protected autoRefresh = signal(false);
  protected rigControl = signal(true);
  protected showDigi = signal(false);
  protected showPhone = signal(true);
  protected showCw = signal(true);
  protected selectedCall = signal('');
  protected selectedParkNum = signal('');
  private refreshInterval?: ReturnType<typeof setInterval>;
  protected qsoEditVisible = model(false); // model() for two-way binding with dialog
  protected huntingStatsVisible = model(false);

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
    this.refreshSpots();
  }

  protected refreshSpots() {
    this._potaSvc.getActivity().subscribe({
      next: (r: PotaActivityStatsModel[]) => {
        this.allSpots.set(r);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  protected onCallSignClick(callSign: string, freqKhz: string, mode: string): void {
    this.selectedCall.set(callSign);
    this.qsoEditVisible.set(true);

    if (!this.rigControl()) {
      return;
    }

    let freq = Number(freqKhz);
    if (mode === 'SSB') {
      mode = freq > 14000 ? 'USB' : 'LSB';
    }
    this._infraSvc.setRigStatus(Number(freqKhz)*1000, mode).subscribe({
      error: e => {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
        this.rigControl.set(false);
      }
    });
  }

  protected onParkClick(parkNum: string): void {
    this.selectedParkNum.set(parkNum);
    this.huntingStatsVisible.set(true);
  }

  protected onQsoSaved() {
    this.qsoEditVisible.set(false);
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
