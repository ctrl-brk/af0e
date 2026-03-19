import {Component, inject, signal, ViewEncapsulation} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {Card} from 'primeng/card';
import {FloatLabel} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {Tooltip} from 'primeng/tooltip';
import {UtilsService} from '../../services/utils.service';
import {Utils} from '../../shared/utils';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {NotificationMessageModel, NotificationMessageSeverity} from '../../shared/notification-message.model';

@Component({
  templateUrl: './utils-grid.component.html',
  styleUrl: './utils-grid.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [Card, FloatLabel, InputText, FormsModule, Button, Tooltip],
})
export class UtilsGridComponent {
  private _ntfSvc = inject(NotificationService);
  private _log = inject(LogService);
  private _utilsSvc = inject(UtilsService);

  latitude: string = '';
  longitude: string = '';
  gridSquare = signal('?');
  inputValid = signal(false);

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pastedText = event.clipboardData?.getData('text') || '';

    // Try to parse as a coordinate pair (e.g., "39.944284, -105.047808")
    const pattern = /^\s*(-?\d+\.?\d*)\s*,\s*(-?\d+\.?\d*)\s*$/;
    const match = pastedText.match(pattern);

    if (match) {
      this.latitude = match[1].trim();
      this.longitude = match[2].trim();
    } else {
      // If not a coordinate pair, paste as is into the focused field
      const target = event.target as HTMLInputElement;
      if (target.id === 'latitude') {
        this.latitude = pastedText.trim();
      } else if (target.id === 'longitude') {
        this.longitude = pastedText.trim();
      }
    }
  }

  calculateGrid(): void {
    // Validate inputs are valid numbers
    const lat = parseFloat(this.latitude);
    const lon = parseFloat(this.longitude);

    if (isNaN(lat) || isNaN(lon)) {
      this.gridSquare.set('Invalid coordinates');
      this.inputValid.set(false);
      return;
    }

    // Validate latitude range (-90 to 90)
    if (lat < -90 || lat > 90) {
      this.gridSquare.set('Latitude must be between -90 and 90');
      this.inputValid.set(false);
      return;
    }

    // Validate longitude range (-180 to 180)
    if (lon < -180 || lon > 180) {
      this.gridSquare.set('Longitude must be between -180 and 180');
      this.inputValid.set(false);
      return;
    }

    this._utilsSvc.coordinatesToGrid(this.latitude, this.longitude).subscribe({
      next: (r) => {
        this.gridSquare.set(r);
        this.inputValid.set(r.length === 6);
      },
      error: e => {
        this.inputValid.set(false);
        if (e.status !== 400)
          Utils.showErrorMessage(e, this._ntfSvc, this._log);
        else
          this.gridSquare.set(e.error);
      }
    });
  }

  copyToClipboard(): void {
    const value = this.gridSquare();
    if (value && value !== '?') {
      navigator.clipboard.writeText(value).then(() => {
        this._ntfSvc.addMessage(new NotificationMessageModel(NotificationMessageSeverity.Info, 'Clipboard', 'Copied to clipboard'));
      }).catch(() => {
        this._ntfSvc.addMessage(new NotificationMessageModel(NotificationMessageSeverity.Error, 'Clipboard', 'Copy failed'));
      });
    }
  }
}
