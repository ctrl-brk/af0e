import {Component, inject, model, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {form, FormField} from '@angular/forms/signals';
import {FloatLabelModule} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {ButtonModule} from 'primeng/button';
import {DatePipe} from '@angular/common';
import {ScrollTop} from 'primeng/scrolltop';
import {TableModule} from 'primeng/table';
import {PotaActivationModel} from '../../../models/pota-activation.model';
import {PotaService} from '../../../services/pota.service';
import {Utils} from '../../../shared/utils';
import {NotificationService} from '../../../shared/notification.service';
import {LogService} from '../../../shared/log.service';
import {Router} from '@angular/router';
import {AppAuthService} from '../../../services/auth.service';
import {Dialog} from 'primeng/dialog';
import {Tooltip} from 'primeng/tooltip';
import {MapboxService} from '../../../services/mapbox.service';
import {activationSchema, initialActivationData, NewActivationFormData} from './new-activation-form-data';
import {NotificationMessageModel, NotificationMessageSeverity} from '../../../shared/notification-message.model';

@Component({
  templateUrl: './activations.component.html',
  styleUrl: './activations.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    ButtonModule,
    DatePipe,
    FloatLabelModule,
    InputText,
    ScrollTop,
    TableModule,
    Dialog,
    FormField,
    Tooltip,
  ],
})
export class PotaActivationsComponent implements OnInit {
  private _router = inject(Router);
  private _potaSvc = inject(PotaService);
  private _mapboxSvc = inject(MapboxService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  protected _authSvc = inject(AppAuthService);
  protected activations = signal<PotaActivationModel[]>([]);
  protected addActivationVisible = model(false);
  protected newActivationModel = signal<NewActivationFormData>(initialActivationData);
  protected newActivationForm = form(this.newActivationModel, activationSchema)

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

  protected onCalculateLocation(): void {
    let lat = parseFloat(this.newActivationForm.lat().value().trim());
    let lon = parseFloat(this.newActivationForm.lon().value().trim());

    if ((isNaN(lat) || isNaN(lon)) && !navigator.geolocation) {
      this._ntfSvc.addMessage(new NotificationMessageModel(NotificationMessageSeverity.Warn, 'Location', 'Geocoding is not supported in this browser.'));
      return;
    }

    if ((isNaN(lat) || isNaN(lon))) {
      navigator.geolocation.getCurrentPosition(
        pos => {
          lat = pos.coords.latitude;
          lon = pos.coords.longitude;
          this.setLocation(lat, lon);
        },
        e => Utils.showErrorMessage(e, this._ntfSvc, this._log))
    }
    else
      this.setLocation(lat, lon);
  }

  private setLocation(lat: number, lon: number) {
    const grid = Utils.latLonToGrid(lat, lon);

    this.newActivationForm.grid().value.set(grid);
    this.newActivationForm.lat().value.set(lat.toString());
    this.newActivationForm.lon().value.set(lon.toString());

    this._mapboxSvc.getLocationByCoordinates(lat, lon).subscribe({
      next: (l) => {
        this.newActivationForm.county().value.set(l.county ?? '');
        this.newActivationForm.state().value.set(l.stateCode ?? '');
      },
      error: (e) => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected onOpenGoogleMaps(): void {
    const lat = this.newActivationForm.lat().value();
    const lon = this.newActivationForm.lon().value();
    if (!lat || !lon) return;
    window.open(`https://www.google.com/maps?q=${lat},${lon}`, '_blank');
  }

  protected onAddActivation() {
    this.addActivationVisible.set(true);
  }

  protected onSaveNewActivation() {
    this._potaSvc.createActivation(this.newActivationModel()).subscribe({
      next: (id: number) => {
        this._router.navigate(['/pota/activations', id]);
      },
      error: (e) => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }
}
