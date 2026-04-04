import {
  Component,
  computed,
  DestroyRef,
  inject,
  model,
  OnInit,
  signal,
  viewChild,
  ViewEncapsulation
} from '@angular/core';
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
import {Button} from 'primeng/button';
import {AppAuthService} from '../../../services/auth.service';
import {Dialog} from 'primeng/dialog';
import {QsoEditComponent} from '../../logbook/qso-edit.component';
import {Checkbox} from 'primeng/checkbox';
import {FloatLabelModule} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {InfraService} from '../../../services/infra.service';
import {PotaAppService} from '../../../services/pota-app.service';
import {PotaActivationInfoComponent} from './activation-info.component';

@Component({
  templateUrl: './activation.component.html',
  styleUrl: './activation.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    Card,
    FloatLabelModule,
    InputText,
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
    Button,
    Dialog,
    QsoEditComponent,
    Checkbox,
    PotaActivationInfoComponent,
  ],
})
export class PotaActivationComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);
  private _potaSvc = inject(PotaService);
  private _infraSvc= inject(InfraService);
  private _potaAppSvc= inject(PotaAppService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  private activationInfo = viewChild(PotaActivationInfoComponent);

  protected _authSvc = inject(AppAuthService);
  protected activationId = signal(0);
  protected logId = signal(-1);
  protected activation = signal<PotaActivationModel>(null!);
  protected logEntries = signal<ActivationQsoModel[]>([]);
  protected qsoEditVisible = model(false);
  protected spotDlgVisible = model(false);
  protected rigControl = signal(false);
  protected keyerControl = signal(false);
  protected rigCommanderConfig = signal<any>({});
  protected spotFreq = signal('14048.00');
  protected spotComment = signal('CQ');

  protected qsoRate = computed(() => {
    const entries = this.logEntries();
    if (entries.length < 2) return null;
    const recent = [...entries]
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
      .slice(0, 10);
    if (recent.length < 2) return null;
    const newest = new Date(recent[0].date).getTime();
    const oldest = new Date(recent[recent.length - 1].date).getTime();
    const hours = (newest - oldest) / 3_600_000;
    if (hours === 0) return null;
    return Math.round(recent.length / hours);
  });

  ngOnInit(): void {
    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {
        this.activationId.set(parseInt(x.get('id')!));
        this.onActivationChange(this.activationId());
      }
    });

    this._infraSvc.getConfig().subscribe({
      next: (r: any) => {
        this.rigCommanderConfig.set(r);
        this.rigControl.set(true);
      },
      error: () => {
        this.rigControl.set(false);
      }
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  private onActivationChange(id: number) {
    this._potaSvc.getActivation(id).subscribe({
      next: (r: PotaActivationModel) => {
        this.activation.set(r);
        if (!r.endDate)
          setTimeout(() => this.activationInfo()?.refresh(), 100);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });

    this.loadActivationLog(id)
  }

  private loadActivationLog(id: number) {
    this._potaSvc.getActivationLog(id).subscribe({
      next: (r: ActivationQsoModel[]) => this.logEntries.set(r),
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  onTabChange(tab: unknown) {

  }

  protected onAddQso() {
    this.logId.set(-1);
    this.qsoEditVisible.set(true);
  }

  protected onQsoSaved($event: any) {
    if (this.logId() > 0)
      this.qsoEditVisible.set(false);

    this.loadActivationLog(this.activationId());
    this.activationInfo()?.refresh();
  }

  protected onQsoSelected(logId: number) {
    this.logId.set(logId);
    this.qsoEditVisible.set(true);
  }

  protected onNewActivationCreated(id: number) {
    this.activationId.set(id);
    this.onActivationChange(id);
    this.onAddQso();
  }

  protected onSpotShow() {
    if (this.rigControl()) {

      this._infraSvc.getRigStatus().subscribe({
        next: (r) => {
          const f = r.frequencyHz.toString().slice(0, -1);
          let f2 = f.slice(-2);
          f2 = f2 === '00' ? '' : `.${f2}`;
          this.spotFreq.set(`${f.slice(0, -2)}${f2}`);
        },
        error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
      });
    }

    this.spotDlgVisible.set(true);
  }

  protected onSpot() {
    this._potaAppSvc.addSpot(this.activation().parkNum, this.spotFreq(), this.spotComment()).subscribe({
      next: () => this.spotDlgVisible.set(false),
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }
}
