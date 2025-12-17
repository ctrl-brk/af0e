import {Component, effect, inject, input, output, untracked, ViewEncapsulation} from '@angular/core';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
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
  ],
})
export class QsoEditComponent {
  private _fb = inject(FormBuilder);
  private _lbSvc = inject(LogbookService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);

  logId = input.required<number>();
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
    this.setupFrequencyToBandMapping();

    effect(() => {
      const id = this.logId();
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
        }
      });
    });
  }

  private setupFrequencyToBandMapping() {
    // Subscribe to frequency changes and auto-select band
    this.qsoForm.get('freq')?.valueChanges.subscribe(freq => {
      if (freq && freq > 0) {
        const band = Utils.getBandFromFrequency(freq);
        if (band) {
          this.qsoForm.get('band')?.setValue(band, { emitEvent: false });
        }
      }
    });
  }

  private initializeForm() {
    this.qsoForm = this._fb.group({
      id: [0],
      date: [Utils.getCurrentUtcDate(), Validators.required],
      call: ['', [Validators.required, callSignValidator()]],
      band: ['', Validators.required],
      freq: [0, Validators.min(0)],
      mode: ['', Validators.required],
      rstSent: ['', Validators.pattern(/^[+-]?[0-9]{2,3}$/)],
      rstRcvd: ['', Validators.pattern(/^[+-]?[0-9]{2,3}$/)],
      myCity: [''],
      myCounty: [''],
      myState: [''],
      myCountry: [''],
      myCqZone: [''],
      myItuZone: [''],
      myGrid: ['', Validators.pattern(/^[A-R]{2}[0-9]{2}([A-X]{2})?$/i)],
      qslSent: ['N'],
      qslSentDate: [null],
      qslSentVia: [null],
      qslRcvd: ['N'],
      qslRcvdDate: [null],
      qslRcvdVia: [null],
      comment: ['', Validators.maxLength(4000)],
      siteComment: ['', Validators.maxLength(64)],
    });
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
      freq: 0,
      mode: '',
      rstSent: '',
      rstRcvd: '',
      myCity: '',
      myCounty: '',
      myState: '',
      myCountry: '',
      myCqZone: '',
      myItuZone: '',
      myGrid: '',
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
    if (field.errors['callSignInvalid']) return field.errors['callSignInvalid'].message;
    if (field.errors['callSignTooShort']) return field.errors['callSignTooShort'].message;
    if (field.errors['callSignNoDigit']) return field.errors['callSignNoDigit'].message;
    if (field.errors['callSignTooManySlashes']) return field.errors['callSignTooManySlashes'].message;
    if (field.errors['callSignEmptyPart']) return field.errors['callSignEmptyPart'].message;
    if (field.errors['pattern']) {
      if (fieldName === 'myGrid') return 'Invalid grid square format (e.g., DN70, DN70ab)';
      if (fieldName === 'rstSent' || fieldName === 'rstRcvd') return 'RST must be 2-3 digits, optionally with +/- prefix (e.g., 59, +599, -73)';
    }
    if (field.errors['min']) return `Minimum value is ${field.errors['min'].min}`;
    if (field.errors['max']) return `Maximum value is ${field.errors['max'].max}`;
    if (field.errors['maxlength']) {
      const limit = field.errors['maxlength'].requiredLength;
      if (fieldName === 'siteComment') return `Site Comment exceeds ${limit} characters.`;
      if (fieldName === 'comment') return `Comment exceeds ${limit} characters.`;
      return `Maximum length is ${limit} characters.`;
    }
    return 'Invalid value';
  }
}
