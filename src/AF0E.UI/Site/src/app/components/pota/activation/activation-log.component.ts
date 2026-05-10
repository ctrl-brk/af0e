import {Component, inject, input, model, OnInit, output, signal, ViewEncapsulation} from '@angular/core';
import {ButtonModule} from 'primeng/button';
import {DatePipe} from '@angular/common';
import {Dialog} from 'primeng/dialog';
import {QsoComponent} from '../../qso/qso.component';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {ActivationQsoModel} from '../../../models/activation-qso.model';
import {ModeSeverityPipe, QsoModePipe} from '../../../shared/pipes';
import {AppAuthService} from '../../../services/auth.service';
import {Utils} from '../../../shared/utils';
import {ContextMenu} from 'primeng/contextmenu';
import {MenuItem} from 'primeng/api';
import {LogbookService} from '../../../services/logbook.service';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {PotaService} from '../../../services/pota.service';
import {PotaActivationModel} from '../../../models/pota-activation.model';

@Component({
  selector: 'app-activation-log',
  templateUrl: './activation-log.component.html',
  styleUrl: './activation-log.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    ButtonModule,
    DatePipe,
    Dialog,
    QsoComponent,
    TableModule,
    Tag,
    ModeSeverityPipe,
    QsoModePipe,
    ContextMenu,
  ],
})
export class PotaActivationLogComponent implements OnInit {
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _lbSvc = inject(LogbookService);
  private _potaSvc = inject(PotaService);
  protected _authSvc = inject(AppAuthService);
  protected cmItems = signal<MenuItem[]>([]);
  protected myCallsign = signal('');
  protected selectedId = signal(0);
  protected qsoDetailsVisible = model(false);

  logEntries = input.required<ActivationQsoModel[]>();
  activation = input.required<PotaActivationModel>();
  protected selectedQso!: ActivationQsoModel;
  qsoSelected = output<number>();
  qsoDeleted = output();

  ngOnInit(): void {
    const adminSub = this._authSvc.hasRoleAsync('Admin').subscribe(isAdmin => {
      this.cmItems.set(isAdmin ? [
        {label: 'Unlink', icon: 'pi pi-link', command: () => { this.onUnlinkQso(this.selectedQso);}},
        {label: 'Delete', icon: 'pi pi-trash', command: () => { this.onDeleteQso(this.selectedQso);}},
      ] : []);
    });
  }

  onQsoSelect(qso: ActivationQsoModel) {
    if (this._authSvc.isAdmin) {
      this.qsoSelected.emit(qso.logId);
      return;
    }

    let call = qso.operatorCallsign ?? Utils.getMyEffectiveCall(qso.date);
    if (qso.stationCallsign && qso.stationCallsign !== call)
      call = `${call} @ ${qso.stationCallsign}`;

    this.myCallsign.set(call);
    this.selectedId.set(qso.logId);
    this.qsoDetailsVisible.set(true);
  }

  private onUnlinkQso(qso: ActivationQsoModel) {
    this._potaSvc.unlinkQso(this.activation().id, qso.logId).subscribe({
      next: () => this.qsoDeleted.emit(),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  private onDeleteQso(qso: ActivationQsoModel) {
    this._lbSvc.deleteQso(qso.logId).subscribe({
      next: () => this.qsoDeleted.emit(),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }
}
