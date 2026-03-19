import {Component, effect, inject, input, signal, ViewEncapsulation} from '@angular/core';
import {TableModule} from 'primeng/table';
import {PotaService} from '../../../../services/pota.service';
import {QsoSummaryModel} from '../../../../models/qso-summary.model';
import {Utils} from '../../../../shared/utils';
import {NotificationService} from '../../../../shared/notification.service';
import {LogService} from '../../../../shared/log.service';
import {Card} from 'primeng/card';
import {DatePipe} from '@angular/common';
import {PotaParkModel} from '../../../../models/pota-park.model';
import {ScrollTop} from 'primeng/scrolltop';
import {GridPipe, ModeSeverityPipe, QsoModePipe} from '../../../../shared/pipes';
import {Tag} from 'primeng/tag';
import {GridMapDirective} from '../../../../shared/directives/grid-map.directive';

@Component({
  selector: 'app-park-hunting-stats',
  templateUrl: './park-hunting-stats.component.html',
  styleUrl: './park-hunting-stats.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    TableModule,
    Card,
    DatePipe,
    ScrollTop,
    ModeSeverityPipe,
    QsoModePipe,
    Tag,
    GridPipe,
    GridMapDirective,
  ],
})
export class ParkHuntingStatsComponent {
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  // Input signal for park number
  parkNum = input.required<string>();

  protected park = signal<PotaParkModel | null>(null);
  protected logEntries = signal<QsoSummaryModel[]>([]);
  protected totalP2P = signal(0);

  constructor() {
    // React to park number changes
    effect(() => {
      const parkNumber = this.parkNum();
      if (parkNumber) {
        this.loadParkData(parkNumber);
      }
    });
  }

  private loadParkData(parkNum: string) {
    this._potaSvc.getPark(parkNum).subscribe({
      next: (r: PotaParkModel) => {
        this.park.set(r);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this._potaSvc.getParkHuntingQsoSummaries(parkNum).subscribe({
      next: (r: QsoSummaryModel[]) => {
        this.logEntries.set(r);
        this.totalP2P.set(r.reduce((acc, x) => acc + x.potaCount, 0));
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }
}

