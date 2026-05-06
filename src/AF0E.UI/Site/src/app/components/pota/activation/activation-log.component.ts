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
  protected _authSvc = inject(AppAuthService);
  protected cmItems: MenuItem[] = [];
  protected myCallsign = signal('');
  protected selectedId = signal(0);
  protected qsoDetailsVisible = model(false);

  logEntries = input.required<ActivationQsoModel[]>();
  protected selectedQso!: ActivationQsoModel;
  qsoSelected = output<number>();
  qsoDeleted = output();

  ngOnInit(): void {
    const adminSub = this._authSvc.hasRoleAsync('Admin').subscribe(isAdmin => {
      this.cmItems = isAdmin ? [{
        label: 'Delete', icon: 'pi pi-trash', command: () => { this.onDeleteQso(this.selectedQso); }
      }] : [];
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

  private onDeleteQso(qso: ActivationQsoModel) {
    this._lbSvc.deleteQso(qso.logId).subscribe({
      next: () => this.qsoDeleted.emit(),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }
}
