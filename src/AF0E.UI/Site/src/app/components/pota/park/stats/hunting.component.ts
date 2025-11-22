import {Component, DestroyRef, inject, OnInit, ViewEncapsulation} from '@angular/core';
import {TableModule} from 'primeng/table';
import {ActivatedRoute} from '@angular/router';
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
import {GridMapDirective} from '../../../../shared/directives';

@Component({
  templateUrl: './hunting.component.html',
  styleUrl: './hunting.component.scss',
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
export class PotaParkHuntingComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  protected parkNum = '';
  protected park: PotaParkModel | null = null;
  protected logEntries: QsoSummaryModel[] = [];
  protected totalP2P = 0;

  ngOnInit(): void {

    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {this.parkNum = x.get('parkNum')!; this.onParkChange(this.parkNum);}
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private onParkChange(parkNum: string) {
    this._potaSvc.getPark(parkNum).subscribe({
      next: (r: PotaParkModel) => {
        this.park = r;
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this._potaSvc.getParkHuntingQsoSummaries(parkNum).subscribe({
      next: (r: QsoSummaryModel[]) => {
        this.logEntries = r;
        this.totalP2P = r.reduce((acc, x) => acc + x.potaCount, 0)
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }
}
