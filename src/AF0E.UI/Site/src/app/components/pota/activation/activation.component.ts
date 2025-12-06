import {Component, DestroyRef, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {PotaActivationModel} from '../../../models/pota-activation.model';
import {PotaService} from '../../../services/pota.service';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {ActivatedRoute} from '@angular/router';
import {DatePipe} from '@angular/common';
import {Tag} from 'primeng/tag';
import {Card} from 'primeng/card';
import {Tab, TabList, TabPanel, TabPanels, Tabs} from 'primeng/tabs';
import {PotaActivationLogComponent} from './activation-log.component';
import {ActivationQsoModel} from '../../../models/activation-qso.model';
import {PotaActivationMapComponent} from './activation-map.component';

@Component({
  templateUrl: './activation.component.html',
  styleUrl: './activation.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    Card,
    FormsModule,
    Tabs,
    TabList,
    Tab,
    TabPanels,
    TabPanel,
    PotaActivationLogComponent,
    Tag,
    DatePipe,
    PotaActivationMapComponent,
  ],
})
export class PotaActivationComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  protected activationId = signal(0);
  protected activation = signal<PotaActivationModel>(null!);
  protected logEntries = signal<ActivationQsoModel[]>([]);

  ngOnInit(): void {
    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {
        this.activationId.set(parseInt(x.get('id')!));
        this.onActivationChange(this.activationId());
      }
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private onActivationChange(id: number) {
    this._potaSvc.getActivation(id).subscribe({
      next: (r: PotaActivationModel) => this.activation.set(r),
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this._potaSvc.getActivationLog(id).subscribe({
      next: (r: ActivationQsoModel[]) => this.logEntries.set(r),
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  onTabChange(tab: unknown) {

  }
}
