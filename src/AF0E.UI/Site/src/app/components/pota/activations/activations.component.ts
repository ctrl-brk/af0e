import {Component, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {AutoCompleteModule} from 'primeng/autocomplete';
import {FloatLabelModule} from 'primeng/floatlabel';
import {MenubarModule} from 'primeng/menubar';
import {ButtonModule} from 'primeng/button';
import {MenuModule} from 'primeng/menu';
import {DatePipe} from '@angular/common';
import {ScrollTop} from 'primeng/scrolltop';
import {TableModule} from 'primeng/table';
import {PotaActivationModel} from '../../../models/pota-activation.model';
import {PotaService} from '../../../services/pota.service';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {Router} from '@angular/router';

@Component({
  templateUrl: './activations.component.html',
  styleUrl: './activations.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    AutoCompleteModule,
    ButtonModule,
    DatePipe,
    FloatLabelModule,
    FormsModule,
    MenuModule,
    MenubarModule,
    ScrollTop,
    TableModule,
  ],
})
export class PotaActivationsComponent implements OnInit {
  private _router = inject(Router);
  private _potaSvc = inject(PotaService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  protected activations = signal<PotaActivationModel[]>([]);

  ngOnInit(): void {
    this._potaSvc.getActivations().subscribe({
      next: (r: PotaActivationModel[]) => {
        this.activations.set(r);
      },
      error: e=> Utils.showErrorMessage(e, this._ntfSvc, this._log),
    });
  }

  onActivationSelect(act: PotaActivationModel) {
    this._router.navigate(['/pota/activations', act.id]);
  }
}
