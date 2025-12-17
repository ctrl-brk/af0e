import {Component, input, model, signal, ViewEncapsulation} from '@angular/core';
import {ButtonModule} from 'primeng/button';
import {DatePipe} from '@angular/common';
import {Dialog} from 'primeng/dialog';
import {QsoComponent} from '../../qso/qso.component';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {ActivationQsoModel} from '../../../models/activation-qso.model';
import {ModeSeverityPipe, QsoModePipe} from '../../../shared/pipes';

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
  ],
})
export class PotaActivationLogComponent {
  protected myCallsign = signal('');
  protected selectedId = signal(0);
  protected qsoDetailsVisible = model(false);

  logEntries = input.required<ActivationQsoModel[]>();

  onQsoSelect(qso: ActivationQsoModel) {
    if (qso.date > new Date(Date.UTC(2011, 0, 6)))
      this.myCallsign.set('AFØE');
    else if (qso.date > new Date(2010, 10, 21))
      this.myCallsign.set('K3OSO');
    else
      this.myCallsign.set('KDØHHE');

    this.selectedId.set(qso.logId);
    this.qsoDetailsVisible.set(true);
  }
}
