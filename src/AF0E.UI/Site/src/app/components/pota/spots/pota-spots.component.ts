import {Component, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {Utils} from '../../../shared/utils';
import {PotaService} from '../../../services/pota.service';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {PotaActivityModel} from '../../../models/pota-activity.model';
import {ScrollTop} from 'primeng/scrolltop';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {ModeSeverityPipe, QsoModePipe, TimeAgoPipe} from '../../../shared/pipes';

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
  ]
})
export class PotaSpotsComponent implements OnInit {
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  protected spots = signal<PotaActivityModel[]>([]);

  ngOnInit(): void {
    this._potaSvc.getActivity().subscribe({
      next: (r: PotaActivityModel[]) => {
        this.spots.set(r);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }
}
