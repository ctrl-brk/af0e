import {
  Component,
  effect,
  ElementRef,
  inject,
  input,
  output,
  untracked,
  viewChild,
  ViewEncapsulation
} from '@angular/core';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {toSignal} from '@angular/core/rxjs-interop';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {LogbookService} from '../../services/logbook.service';
import {QsoDetailModel} from '../../models/qso-detail.model';
import {Utils} from '../../shared/utils';
import {NotificationMessageModel, NotificationMessageSeverity} from '../../shared/notification-message.model';
import {FloatLabelModule} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {InputNumber} from 'primeng/inputnumber';
import {DatePicker} from 'primeng/datepicker';
import {Select} from 'primeng/select';
import {Button} from 'primeng/button';
import {Textarea} from 'primeng/textarea';
import {Fieldset} from 'primeng/fieldset';
import {Tooltip} from 'primeng/tooltip';
import {callSignValidator} from '../../shared/validators';
import {BAND_OPTIONS, MODE_OPTIONS, QSL_OPTIONS, QSL_VIA_OPTIONS} from '../../shared/qso-options';
import {PotaService} from '../../services/pota.service';
import {QrzService} from '../../services/qrz.service';
import {QrzDetailsModel} from '../../models/qrz-details.model';
import {SpaceAsTabDirective} from '../../shared/directives/space-as-tab.directive';

@Component({
  selector: 'app-qso-edit',
  templateUrl: './qso-edit.component.html',
  styleUrl: './qso-edit.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    ReactiveFormsModule,
    FloatLabelModule,
    InputText,
    InputNumber,
    DatePicker,
    Select,
    Button,
    Textarea,
    Fieldset,
    Tooltip,
    SpaceAsTabDirective,
  ],
})
export class QsoEditComponent {
  private _fb = inject(FormBuilder);
  private _lbSvc = inject(LogbookService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _potaSvc = inject(PotaService);
  private _qrzSvc = inject(QrzService);
  private _callInput = viewChild<ElementRef>('callInput');
  private _rstRcvdInput = viewChild<ElementRef>('rstRcvdInput');

  logId = input.required<number>();
  callSign = input<string>();
  editModeChange = output<boolean>();
  saved = output<boolean>();

  protected qso: QsoDetailModel = null!;
  protected qsoForm!: FormGroup;
  isEditMode = false;

  // Dropdown options
  protected modeOptions = MODE_OPTIONS;
  protected bandOptions = BAND_OPTIONS;
  protected qslOptions = QSL_OPTIONS;
  protected qslViaOptions = QSL_VIA_OPTIONS;

  constructor() {
    this.initializeForm();

    // Convert mode field valueChanges to a signal
    const modeSignal = toSignal(this.qsoForm.get('mode')!.valueChanges);

    // Watch for mode changes and auto-adjust RST values
    effect(() => {
      const mode = modeSignal();
      if (mode) {
        untracked(() => {
          this.adjustRstForMode(mode);
        });
      }
    });

    effect(() => {
      const id = this.logId();
      const call = this.callSign();
      const callInputEl = this._callInput();

      // Use untracked to prevent form operations from triggering this effect again
      untracked(() => {
        if (id > 0) {
          this.isEditMode = true;
          this.editModeChange.emit(true);
          this.onQsoChange(id);
        } else {
          this.isEditMode = false;
          this.editModeChange.emit(false);
          this.initializeNewQso();

          if (call)
            this.qsoForm.patchValue({ call });
        }

        // Focus the call input field after a short delay to ensure DOM is ready
        if (callInputEl?.nativeElement) {
          setTimeout(() => {
            callInputEl.nativeElement.focus();
            callInputEl.nativeElement.select();
          }, 0);
        }
      });
    });
  }

  private initializeForm() {
    const defaults = this.getDefaultFormValues();

    this.qsoForm = this._fb.group({
      id: [defaults.id],
      date: [Utils.getCurrentUtcDate(), Validators.required],
      call: [defaults.call, [Validators.required, callSignValidator()]],
      band: [defaults.band, Validators.required],
      county: [defaults.country, Validators.maxLength(32)],
      state: [defaults.state, Validators.maxLength(2)],
      country: [defaults.country, Validators.maxLength(64)],
      freq: [defaults.freq, [Validators.required, Validators.min(1.8), Validators.max(450000000)]],
      grid: [defaults.grid, Validators.pattern(/^[A-R]{2}[0-9]{2}([A-X]{2})?$/i)],
      mode: [defaults.mode, Validators.required],
      rstSent: [defaults.rstSent, Validators.pattern(/^[+-]?[0-9]{2,3}$/)],
      rstRcvd: [defaults.rstRcvd, Validators.pattern(/^[+-]?[0-9]{2,3}$/)],
      name_fmt: [defaults.name_fmt, Validators.maxLength(128)],
      cqZone: [defaults.cqZone, Validators.pattern(/^[0-9]{1,2}$/)],
      ituZone: [defaults.ituZone, Validators.pattern(/^[0-9]{1,2}$/)],
      dxcc: [defaults.dxcc, Validators.pattern(/^[0-9]{3}$/)],
      myCity: [defaults.myCity, Validators.maxLength(32)],
      myCounty: [defaults.myCounty, Validators.maxLength(32)],
      myState: [defaults.myState, Validators.maxLength(2)],
      myCountry: [defaults.myCountry, Validators.maxLength(64)],
      myCqZone: [defaults.myCqZone, Validators.pattern(/^[0-9]{1,2}$/)],
      myItuZone: [defaults.myItuZone, Validators.pattern(/^[0-9]{1,2}$/)],
      myGrid: [defaults.myGrid, Validators.pattern(/^[A-R]{2}[0-9]{2}([A-X]{2})?$/i)],
      qslSent: [defaults.qslSent],
      qslSentDate: [defaults.qslSentDate],
      qslSentVia: [defaults.qslSentVia],
      qslRcvd: [defaults.qslRcvd],
      qslRcvdDate: [defaults.qslRcvdDate],
      qslRcvdVia: [defaults.qslRcvdVia],
      comment: [defaults.comment, Validators.maxLength(4000)],
      siteComment: [defaults.siteComment, Validators.maxLength(64)],
    });
  }

  private adjustRstForMode(mode: string) {
    if (!mode) return;

    const rstRcvdControl = this.qsoForm.get('rstRcvd');
    const rstSentControl = this.qsoForm.get('rstSent');

    const rstRcvdValue = rstRcvdControl?.value?.trim() || '';
    const rstSentValue = rstSentControl?.value?.trim() || '';

    if (mode === 'CW') {
      // For CW: if empty or "59", set to "599"
      if (rstRcvdValue === '' || rstRcvdValue === '59') {
        rstRcvdControl?.setValue('599', { emitEvent: false });
      }
      if (rstSentValue === '' || rstSentValue === '59') {
        rstSentControl?.setValue('599', { emitEvent: false });
      }
    } else if (mode === 'SSB') {
      // For SSB: if empty or "599", set to "59"
      if (rstRcvdValue === '' || rstRcvdValue === '599') {
        rstRcvdControl?.setValue('59', { emitEvent: false });
      }
      if (rstSentValue === '' || rstSentValue === '599') {
        rstSentControl?.setValue('59', { emitEvent: false });
      }
    }
  }

  private onQsoChange(id: number) {
    this._lbSvc.getQso(id).subscribe({
      next: (r: QsoDetailModel) => {
        this.qso = r;
        this.qsoForm.patchValue({
          ...r,
          date: Utils.utcStringToDate(r.date),
          qslSentDate: Utils.utcStringToDate(r.qslSentDate),
          qslRcvdDate: Utils.utcStringToDate(r.qslRcvdDate),
        });
        this.qsoForm.markAsPristine();
      },
      error: e=> {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      },
    });
  }

  protected onFreqBlur(){
    const freq = this.qsoForm.get('freq')?.value;

    if (!freq || freq < 1.8 || freq > 450000000) return;

    let freq1 = freq;
    if (freq < 1000) freq1 *= 1000000;
    else if (freq < 1000000) freq1 *= 100;
    if (freq1 !== freq)
      this.qsoForm.get('freq')?.setValue(freq1, { emitEvent: false });

    this.qsoForm.get('band')?.setValue(Utils.getBandFromFrequency(freq1), { emitEvent: false });
  }

  protected setCurrentDateTime() {
    this.qsoForm.get('date')?.setValue(Utils.getCurrentUtcDate());
    this.qsoForm.markAsDirty();
  }

  onCallBlur() {
    const callControl = this.qsoForm.get('call');
    if (!callControl || callControl.invalid) {
      return;
    }

    const callSign = (callControl.value || '').trim();
    if (!callSign) {
      return;
    }

    this._potaSvc.getActivityByCall(callSign).subscribe({
      next: (r) => {
        if (!r.active)
          return;

        this.qsoForm.get('comment')?.setValue(`POTA ${r.parkNum}`);
        this.qsoForm.get('freq')?.setValue(r.freqHz);
        this.qsoForm.get('mode')?.setValue(r.mode);
        this.qsoForm.get('band')?.setValue(Utils.getBandFromFrequency(r.freqHz), {emitEvent: false});

        this.qsoForm.markAsDirty();
      },
      error: e => {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    });

    this._qrzSvc.lookup(callSign).subscribe({
      next: (r) => {
        this.populateQrzDetails(r);
        // @ts-ignore
        this._rstRcvdInput().nativeElement.select();
      },
      error: e => {
        if (e.status !== 404)
          Utils.showErrorMessage(e, this._ntfSvc, this._log);
        else
          // @ts-ignore
          this._rstRcvdInput().nativeElement.select();
      }
    });
  }

  protected onCallKeyDown(event: KeyboardEvent) {
    // Handle Tab key - trigger lookup
    if (event.key === 'Tab') {
      this.onCallBlur();
    }
  }

  private populateQrzDetails(r: QrzDetailsModel) {
    const name_fmt = r.name_fmt || '';
    const country = r.country || '';
    const grid = r.grid || '';
    const county = r.county || '';
    const state = r.state || '';
    const cqzone = r.cqzone || '';
    const ituzone = r.ituzone || '';
    const dxcc = r.dxcc || '';

    if (name_fmt && !this.qsoForm.get('name_fmt')?.value) {
      this.qsoForm.get('name_fmt')?.setValue(name_fmt);
      this.qsoForm.markAsDirty();
    }
    if (country && !this.qsoForm.get('country')?.value) {
      this.qsoForm.get('country')?.setValue(country);
      this.qsoForm.markAsDirty();
    }
    if (grid && !this.qsoForm.get('grid')?.value) {
      this.qsoForm.get('grid')?.setValue(grid);
      this.qsoForm.markAsDirty();
    }
    if (county && !this.qsoForm.get('county')?.value) {
      this.qsoForm.get('county')?.setValue(county);
      this.qsoForm.markAsDirty();
    }
    if (state && !this.qsoForm.get('state')?.value) {
      this.qsoForm.get('state')?.setValue(state);
      this.qsoForm.markAsDirty();
    }
    if (cqzone && !this.qsoForm.get('cqZone')?.value) {
      this.qsoForm.get('cqZone')?.setValue(cqzone);
      this.qsoForm.markAsDirty();
    }
    if (ituzone && !this.qsoForm.get('ituZone')?.value) {
      this.qsoForm.get('ituZone')?.setValue(ituzone);
      this.qsoForm.markAsDirty();
    }
    this.qsoForm.get('dxcc')?.setValue(dxcc);
  }

  private initializeNewQso() {
    // Reset form to default values for new QSO
    // First reset without values to clear the form state
    this.qsoForm.reset();
    // Then patch with default values to ensure dropdowns work properly
    this.qsoForm.patchValue(this.getDefaultFormValues());
    this.qsoForm.markAsPristine();
  }

  onSave() {
    if (this.qsoForm.invalid) {
      this._ntfSvc.addMessage(
        new NotificationMessageModel(
          NotificationMessageSeverity.Warn,
          'Validation Error',
          'Please fix all validation errors before saving.'
        )
      );
      this.markAllAsTouched();
      return;
    }

    // Convert date fields to UTC strings for API submission
    const formValue = {
      ...this.qsoForm.value,
      date: Utils.dateToUtcString(this.qsoForm.value.date),
      qslSentDate: Utils.dateToUtcString(this.qsoForm.value.qslSentDate),
      qslRcvdDate: Utils.dateToUtcString(this.qsoForm.value.qslRcvdDate),
    };

    if (this.isEditMode) {
      this._lbSvc.updateQso(formValue).subscribe({
        next: () => {
          this.saved.emit(this.isEditMode);
        },
        error: e => { Utils.showErrorMessage(e, this._ntfSvc, this._log); }
      });

      return;
    }

    this._lbSvc.createQso(formValue).subscribe({
      next: () => {
        this.saved.emit(this.isEditMode);
      },
      error: e => { Utils.showErrorMessage(e, this._ntfSvc, this._log); }
    });
  }

  onClear() {
    if (this.isEditMode && this.qso) {
      // In edit mode, reset to original values
      this.qsoForm.patchValue({
        ...this.qso,
        date: Utils.utcStringToDate(this.qso.date),
        qslSentDate: Utils.utcStringToDate(this.qso.qslSentDate),
        qslRcvdDate: Utils.utcStringToDate(this.qso.qslRcvdDate),
      });

      this.qsoForm.markAsPristine();
    } else {
      // In creation mode, reset to an empty form
      this.initializeNewQso();
    }
  }

  private getDefaultFormValues() {
    return {
      id: 0,
      date: Utils.getCurrentUtcDate(),
      call: '',
      band: '',
      country: '',
      county: '',
      state: '',
      freq: undefined,
      grid: '',
      mode: '',
      rstSent: '',
      rstRcvd: '',
      name_fmt: '',
      cqZone: '',
      ituZone: '',
      dxcc: '',
      myCity: 'Broomfield',
      myCounty: 'Broomfield',
      myState: 'CO',
      myCountry: 'United States',
      myCqZone: '4',
      myItuZone: '7',
      myGrid: 'DM79lw',
      qslSent: 'N',
      qslSentDate: null,
      qslSentVia: null,
      qslRcvd: 'N',
      qslRcvdDate: null,
      qslRcvdVia: null,
      comment: '',
      siteComment: '',
    };
  }

  private markAllAsTouched() {
    Object.keys(this.qsoForm.controls).forEach(key => {
      this.qsoForm.get(key)?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.qsoForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getErrorMessage(fieldName: string): string {
    const field = this.qsoForm.get(fieldName);
    if (!field || !field.errors) return '';
    if (field.errors['required']) return 'This field is required';
    if (fieldName === 'call' && field.errors['callSignInvalid']) return field.errors['callSignInvalid'].message;
    if (fieldName === 'call' && field.errors['callSignTooShort']) return field.errors['callSignTooShort'].message;
    if (fieldName === 'call' && field.errors['callSignNoDigit']) return field.errors['callSignNoDigit'].message;
    if (fieldName === 'call' && field.errors['callSignTooManySlashes']) return field.errors['callSignTooManySlashes'].message;
    if (fieldName === 'call' && field.errors['callSignEmptyPart']) return field.errors['callSignEmptyPart'].message;
    if (field.errors['pattern']) {
      if (fieldName === 'grid' || fieldName === 'myGrid') return 'Invalid grid square format (e.g., DN70, DN70ab)';
      if (fieldName === 'rstSent' || fieldName === 'rstRcvd') return 'RST must be 2-3 digits, optionally with +/- prefix (e.g., 59, +599, -73)';
    }
    if (field.errors['min']) return `Minimum value is ${field.errors['min'].min}`;
    if (field.errors['max']) return `Maximum value is ${field.errors['max'].max}`;
    if (field.errors['maxlength']) {
      const limit = field.errors['maxlength'].requiredLength;
      if (fieldName === 'name_fmt') return `Name exceeds ${limit} characters.`;
      if (fieldName === 'county') return `County exceeds ${limit} characters.`;
      if (fieldName === 'state') return `State exceeds ${limit} characters.`;
      if (fieldName === 'country') return `Country exceeds ${limit} characters.`;
      if (fieldName === 'siteComment') return `Site Comment exceeds ${limit} characters.`;
      if (fieldName === 'comment') return `Comment exceeds ${limit} characters.`;
      return `Maximum length is ${limit} characters.`;
    }
    return 'Invalid value';
  }
}
