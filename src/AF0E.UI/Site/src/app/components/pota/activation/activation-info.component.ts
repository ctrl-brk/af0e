import {Component, computed, inject, input, signal, ViewEncapsulation} from '@angular/core';
import {DatePipe} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {Checkbox} from 'primeng/checkbox';
import {PotaAppService} from '../../../services/pota-app.service';
import {PotaActivationModel} from '../../../models/pota-activation.model';
import {PotaAppSpotHistoryModel} from '../../../models/pota-app-spot-history.model';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';

@Component({
  selector: 'app-activation-info',
  templateUrl: './activation-info.component.html',
  styleUrl: './activation-info.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [DatePipe, FormsModule, Checkbox],
})
export class PotaActivationInfoComponent {
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _potaAppSvc= inject(PotaAppService);
  protected spotHistory = signal<PotaAppSpotHistoryModel[]>([]);
  protected showRbn = signal(false);
  protected filteredSpots = computed(() =>
    this.showRbn()
      ? this.spotHistory()
      : this.spotHistory().filter(s => s.source !== 'RBN')
  );

  public activation = input.required<PotaActivationModel>();

  public refresh() {
    this._potaAppSvc.getSpotHistory(this.activation().parkNum).subscribe({
      next: r => this.spotHistory.set(r),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    })
  }
}
