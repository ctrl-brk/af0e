import {Component, DestroyRef, inject, OnInit, ViewEncapsulation} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {AutoCompleteModule} from 'primeng/autocomplete';
import {FloatLabelModule} from 'primeng/floatlabel';
import {MenubarModule} from 'primeng/menubar';
import {ButtonModule} from 'primeng/button';
import {MenuModule} from 'primeng/menu';
import {PotaActivationModel} from '../../../models/pota-activation.model';
import {PotaService} from '../../../services/pota.service';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {ActivatedRoute} from '@angular/router';
import {DatePipe} from '@angular/common';
import {Dialog} from 'primeng/dialog';
import {QsoComponent} from '../../qso/qso.component';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {ActivationQsoModel} from '../../../models/activation-qso.model';
import {Card} from 'primeng/card';

@Component({
  standalone: true,
  templateUrl: './activation.component.html',
  styleUrl: './activation.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    AutoCompleteModule,
    ButtonModule,
    Card,
    DatePipe,
    Dialog,
    FloatLabelModule,
    FormsModule,
    MenuModule,
    MenubarModule,
    QsoComponent,
    TableModule,
    Tag,
  ],
})
export class PotaActivationComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _id = 0;

  protected activation: PotaActivationModel = null!;
  protected logEntries: ActivationQsoModel[] = [];
  protected myCallsign = '';
  protected selectedId?: number;
  protected qsoDetailsVisible = false;

  ngOnInit(): void {
    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {this._id = parseInt(x.get('id')!); this.onActivationChange(this._id);}
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private onActivationChange(id: number) {
    this._potaSvc.getActivation(id).subscribe({
      next: (r: PotaActivationModel) => this.activation = r,
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this._potaSvc.getActivationLog(id).subscribe({
      next: (r: ActivationQsoModel[]) => this.logEntries = r,
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  getMode(mode: string) {
    switch (mode) {
      case 'USB':
      case 'LSB':
        return 'SSB';

      case 'MFSK':
        return 'FT4';
    }
    return mode;
  }

  getModeSeverity(mode: string) {
    switch (mode) {
      case 'CW':
        return 'success';

      case 'SSB':
      case 'LSB':
      case 'USB':
        return 'info';

      case 'FT8':
      case 'MFSK':
      case 'PSK31':
      case 'JT65':
        return 'warn';
    }
    return 'secondary';
  }

  onQsoSelect(qso: ActivationQsoModel) {
    if (qso.date > new Date(Date.UTC(2011, 0, 6)))
      this.myCallsign = 'AFØE';
    else if (qso.date > new Date(2010, 10, 21))
      this.myCallsign = 'K3OSO';
    else
      this.myCallsign = 'KDØHHE';

    this.selectedId = qso.logId;
    this.qsoDetailsVisible = true;
  }
}
