import {
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  input,
  model,
  OnInit,
  output,
  signal,
  untracked,
  viewChild,
  ViewEncapsulation
} from '@angular/core';
import {FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators} from '@angular/forms';
import {toSignal} from '@angular/core/rxjs-interop';
import {NotificationService} from '../../shared/notification.service';
import {LogService} from '../../shared/log.service';
import {LogbookService} from '../../services/logbook.service';
import {IdbService} from '../../services/idb.service';
import {InfraService} from '../../services/infra.service';
import {PotaService} from '../../services/pota.service';
import {QrzService} from '../../services/qrz.service';
import {QsoDetailModel} from '../../models/qso-detail.model';
import {Utils} from '../../shared/utils';
import {NotificationMessageModel, NotificationMessageSeverity} from '../../shared/notification-message.model';
import {FloatLabelModule} from 'primeng/floatlabel';
import {InputText} from 'primeng/inputtext';
import {InputNumber} from 'primeng/inputnumber';
import {DatePicker} from 'primeng/datepicker';
import {Select} from 'primeng/select';
import {Button} from 'primeng/button';
import {RadioButton} from 'primeng/radiobutton';
import {Textarea} from 'primeng/textarea';
import {Fieldset} from 'primeng/fieldset';
import {Tooltip} from 'primeng/tooltip';
import {callSignValidator} from '../../shared/validators';
import {BAND_OPTIONS, MODE_OPTIONS, QSL_OPTIONS, QSL_VIA_OPTIONS} from '../../shared/qso-options';
import {QrzDetailsModel} from '../../models/qrz-details.model';
import {SpaceAsTabDirective} from '../../shared/directives/space-as-tab.directive';
import {PotaActivityModel} from '../../models/pota-activity.model';
import {GridTrackerLookupModel} from '../../models/gridtracker-lookup.model';
import {TableModule} from 'primeng/table';
import {DatePipe} from '@angular/common';
import {Dialog} from 'primeng/dialog';
import {Checkbox, CheckboxChangeEvent} from 'primeng/checkbox';
import {PotaActivationModel} from '../../models/pota-activation.model';
import {NewActivationFormData} from '../pota/activations/new-activation-form-data';
import {QsoEditMode} from '../../shared/qso-edit-mode.enum';

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
    FormsModule,
    TableModule,
    DatePipe,
    Dialog,
    RadioButton,
    Checkbox,

  ],
})
export class QsoEditComponent implements OnInit {
  private _fb = inject(FormBuilder);
  private _lbSvc = inject(LogbookService);
  private _idbSvc = inject(IdbService);
  private _ntfSvc= inject(NotificationService);
  private _log = inject(LogService);
  private _potaSvc = inject(PotaService);
  private _qrzSvc = inject(QrzService);
  private _infraSvc = inject(InfraService);
  private _callInput = viewChild<ElementRef>('callInput');
  private _rstSentInput = viewChild<ElementRef>('rstSentInput');
  private _rstRcvdInput = viewChild<ElementRef>('rstRcvdInput');
  private _lastCallsign = '';

  logId = input.required<number>();
  callSign = input<string>();
  keyerControl = input<boolean>(false);
  editMode = input<QsoEditMode>(QsoEditMode.View);
  rigControl = model(false);
  filtersEnabled = input(false);
  potaActivation = input<PotaActivationModel|null>(null);

  editModeChange = output<boolean>();
  saved = output<any>();
  activationCreated = output<number>();
  formInit = output<{call: string, qsoCount: number, DE: {grid: string, city: string, county: string, state: string}}>();

  protected qso: QsoDetailModel = null!;
  protected qsoForm!: FormGroup;
  isEditMode = false;
  protected imgUrl = signal('');
  protected cwCallLabel = signal('???');
  protected cwSending = signal(false);
  protected cqSending = signal(false);
  protected cwSpeed = signal(22);
  protected callHistory = signal<GridTrackerLookupModel[]>([]);
  protected cwTextVisible = signal(false);
  protected readonly QsoEditMode = QsoEditMode;

  // Dropdown options
  protected modeOptions = MODE_OPTIONS;
  protected bandOptions = BAND_OPTIONS;
  protected qslOptions = QSL_OPTIONS;
  protected qslViaOptions = QSL_VIA_OPTIONS;

  protected f3CwMsg = signal<{def: string, alt: string, ctl: string, f6: string}>({def: '', alt: '', ctl: '', f6: ''});
  protected f1Title = 'cq POTA de AFØE AFØE k';
  protected f2Title = 'r 73 ee\nr 73 de AFØE [alt]';
  protected f3Title = computed(() => {return `${this.f3CwMsg().def.replaceAll('|', '')} k\n${this.f3CwMsg().alt.replaceAll('|', '')} k [alt]\n${this.f3CwMsg().ctl.replaceAll('|', '')} k [ctl]\n${this.f3CwMsg().f6.replaceAll('|', '')} k [F6]`});
  protected f8Title = '?\nAGN? [alt]';

  constructor() {
    this.initializeForm();

    const callSignal = toSignal<string>(this.qsoForm.get('call')!.valueChanges);
    const freqSignal = toSignal(this.qsoForm.get('freq')!.valueChanges);
    const modeSignal = toSignal<string>(this.qsoForm.get('mode')!.valueChanges);
    const rstSentSignal = toSignal<string>(this.qsoForm.get('rstSent')!.valueChanges);
    const stateSignal = toSignal<string>(this.qsoForm.get('state')!.valueChanges);
    const nameSignal = toSignal<string>(this.qsoForm.get('name_fmt')!.valueChanges);
    const filterSignal = toSignal(this.qsoForm.get('radioFilter')!.valueChanges);

    effect(() => {
      const call = callSignal();
      this.cwCallLabel.set(call ? call.toUpperCase() : '???');
    });

    effect(() => {
      const mode = modeSignal();
      if (mode)
        this.adjustRstForMode(mode);
    });

    effect(() => {
      this.setExchangeText(rstSentSignal()!, stateSignal()!, nameSignal()!);
    });

    effect(() => {
      const id = this.logId();
      const call = this.callSign();

      // Use untracked to prevent form operations from triggering this effect again
      untracked(() => {
        if (id > 0) {
          this.isEditMode = true;
          this.editModeChange.emit(true);
          this.onQsoChange(id);
        } else {
          this.isEditMode = false;
          this.editModeChange.emit(false);
          this.initializeNewQso(false);

          if (call) {
            this.qsoForm.patchValue({call});
            this.emitFormInit();
          }
        }

        this.setCallFocus(true);
      });
    });

    effect(() => {
      const ctrl = this.qsoForm.get('radioFilter')!;

      if (freqSignal() && modeSignal()) {
        ctrl.setValue('1', { emitEvent: false });
        ctrl.enable();
      }
      else
        ctrl.disable();
    });

    effect(() => {
      if (!this.rigControl()) return;

      let freq = this.qsoForm.get('freq')?.value;
      let mode = this.qsoForm.get('mode')?.value;
      const filter = parseInt(filterSignal());

      if (!freq || !mode) return;

      if (mode === "SSB") {
        freq = parseInt(freq);
        mode = freq <= 4000000 || (freq >= 7000000 && freq <= 7300000) ? "LSB" : "USB";
      }

      this._infraSvc.setRigStatus(freq, mode, filter).subscribe({
        error: e => {
          this.rigControl.set(false);
          Utils.showErrorMessage(e, this._ntfSvc, this._log);
        }
      });
    });
  }

  ngOnInit(): void {
    if (this.potaActivation() && this.rigControl()) {
      this.getRadioStatus();
    }
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
      dxcc: [defaults.dxcc, Validators.pattern(/^[0-9]{1,3}$/)],
      myCity: [defaults.myCity, Validators.maxLength(32)],
      myCounty: [defaults.myCounty, Validators.maxLength(32)],
      myState: [defaults.myState, Validators.maxLength(2)],
      myCountry: [defaults.myCountry, Validators.maxLength(64)],
      myCqZone: [defaults.myCqZone, Validators.pattern(/^[0-9]{1,2}$/)],
      myItuZone: [defaults.myItuZone, Validators.pattern(/^[0-9]{1,2}$/)],
      myGrid: [defaults.myGrid, Validators.pattern(/^[A-R]{2}[0-9]{2}([A-X]{2})?$/i)],
      stationCallsign: [defaults.stationCallsign, [Validators.required, callSignValidator()]],
      operatorCallsign: [defaults.operatorCallsign, [Validators.required, callSignValidator()]],
      qslSent: [defaults.qslSent],
      qslSentDate: [defaults.qslSentDate],
      qslSentVia: [defaults.qslSentVia],
      qslRcvd: [defaults.qslRcvd],
      qslRcvdDate: [defaults.qslRcvdDate],
      qslRcvdVia: [defaults.qslRcvdVia],
      comment: [defaults.comment, Validators.maxLength(4000)],
      siteComment: [defaults.siteComment, Validators.maxLength(64)],
      radioFilter: [defaults.radioFilter],
    });
  }

  private adjustRstForMode(mode: string) {
    if (!mode) return;

    const rstSentControl = this.qsoForm.get('rstSent');
    const rstRcvdControl = this.qsoForm.get('rstRcvd');

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

  private setExchangeText(rstSent: string, state: string, name: string) {
    let greet = Utils.getTimeOfDay(state) + Utils.extractNameOrNickname(name);
    const rst = rstSent ? rstSent.replaceAll('9', 'n') : '5nn';

    const cwText = {...untracked(() => this.f3CwMsg())};

    if (this.editMode() === QsoEditMode.PotaHunting) {
      cwText.def = `R TU ${rst} C|O`;
      cwText.alt = `R ${greet} UR ${rst} C|O`;
      cwText.ctl = `R TU ${rst} ${rst} C|O C|O`;
      cwText.f6 = '';

      this.f3CwMsg.set(cwText);
    }
    else if (this.editMode() === QsoEditMode.PotaActivating) {
      const call = this.cwCallLabel() ? this.cwCallLabel() : '';

      cwText.def = `${call} TU ${rst} C|O`;
      cwText.alt = `${call} ${greet} UR ${rst} C|O`;
      cwText.ctl = `${call} TU ${rst} ${rst} C|O C|O`;
      cwText.f6 = `TU ${rst} C|O`;

      this.f3CwMsg.set(cwText);
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
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
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

    this.qsoForm.get('band')?.setValue(Utils.getBandFromFrequency(freq1), {emitEvent: false});
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

    if (this._lastCallsign == callSign.toLowerCase())
      return;

    this.emitFormInit();
    this._lastCallsign = callSign.toLowerCase();

    if (this.editMode() === QsoEditMode.PotaHunting) {
      this._potaSvc.getActivityByCall(callSign).subscribe({
        next: (r) => {
          if (!r.active)
            this.getRadioStatus();
          else
            this.populatePotaDetails(r);
        },
        error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
      });
    }

    this.imgUrl.set('progress.gif');
    this._qrzSvc.lookup(callSign).subscribe({
      next: (r) => {
          this.populateQrzDetails(r);
        // @ts-ignore
        setTimeout(() => this._rstSentInput().nativeElement.select(), 1);
      },
      error: e => {
        if (e.status !== 404)
          Utils.showErrorMessage(e, this._ntfSvc, this._log);
        else {
          this.imgUrl.set('question.jpg');
          // @ts-ignore
          setTimeout(() => this._rstSentInput().nativeElement.select(), 1);
        }
      }
    });

    this._lbSvc.getGridTrackerLog(callSign).subscribe({
      next: (r) => { this.callHistory.set(r); this.emitFormInit(); },
      error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });
  }

  protected onCallKeyDown(event: KeyboardEvent) {
    // Handle Tab key - trigger lookup
    if (event.key === 'Tab') {
      this.onCallBlur();
    }
  }

  protected onRstSentInput() {
    const mode = this.qsoForm.get('mode')?.value as string;
    const rst: string = this.qsoForm.get('rstSent')?.value || '';
    const targetLength = (mode === 'CW' || mode.startsWith('FT')) ? 3 : 2;
    if (rst.length === targetLength) {
      const el = this._rstRcvdInput()?.nativeElement;
      if (el) { el.focus(); el.select(); }
    }
  }

  private populatePotaDetails(r: PotaActivityModel) {
    if (!r.active)
      return;

    this.qsoForm.get('comment')?.setValue(`POTA ${r.parkNum}`);
    this.qsoForm.get('freq')?.setValue(r.freqHz);
    this.qsoForm.get('mode')?.setValue(r.mode);
    this.qsoForm.get('band')?.setValue(Utils.getBandFromFrequency(r.freqHz), {emitEvent: false});
    this.qsoForm.get('grid')?.setValue(r.grid);

    const locations = r.location?.split(',');
    const location = locations?.[0];
    if (location?.startsWith('US') || location?.startsWith('CA')) {
      this.qsoForm.get('state')?.setValue(location.substring(3));
    }

    this.qsoForm.markAsDirty();
  }

  private populateQrzDetails(r: QrzDetailsModel) {
    this.imgUrl.set(r.image ? r.image : 'https://static.qrz.com/static/qrz/qrz_com.svgz');

    if (this.editMode() === QsoEditMode.Edit) return;

    const name_fmt = r.name_fmt || '';
    const country = r.country || '';
    const grid = r.grid || '';
    const county = r.county || '';
    const state = r.state || '';
    const cqzone = r.cqzone || '';
    const ituzone = r.ituzone || '';
    const dxcc = r.dxcc || '';

    this.qsoForm.get('name_fmt')?.setValue(name_fmt);
    this.qsoForm.get('country')?.setValue(country);

    if (this.editMode() !== QsoEditMode.PotaHunting) {
      this.qsoForm.get('county')?.setValue(county);
      this.qsoForm.get('grid')?.setValue(grid);
      this.qsoForm.get('state')?.setValue(state);
    }

    this.qsoForm.get('cqZone')?.setValue(cqzone);
    this.qsoForm.get('ituZone')?.setValue(ituzone);
    this.qsoForm.get('dxcc')?.setValue(dxcc);

    this.qsoForm.markAsDirty();


  }

  private initializeNewQso(keepFreq: boolean) {
    const freq = this.qsoForm.get('freq')?.value;
    const band = this.qsoForm.get('band')?.value;
    const mode = this.qsoForm.get('mode')?.value;
    // const rstSent = this.qsoForm.get('rstSent')?.value;
    // const rstRcvd = this.qsoForm.get('rstRcvd')?.value;
    const myGrid = this.qsoForm.get('myGrid')?.value;
    const myCity = this.qsoForm.get('myCity')?.value;
    const myCounty = this.qsoForm.get('myCounty')?.value;
    const myState = this.qsoForm.get('myState')?.value;

    // Reset form to default values for new QSO
    // First reset without values to clear the form state
    this.qsoForm.reset();
    // Then patch with default values to ensure dropdowns work properly
    this.qsoForm.patchValue(this.getDefaultFormValues());

    if (keepFreq) {
      this.qsoForm.get('freq')?.setValue(freq);
      this.qsoForm.get('band')?.setValue(band);
      this.qsoForm.get('mode')?.setValue(mode);
      const rst = mode === 'CW' ? '599' : '59';
      this.qsoForm.get('rstSent')?.setValue(rst);
      this.qsoForm.get('rstRcvd')?.setValue(rst);
      this.qsoForm.get('myGrid')?.setValue(myGrid);
      this.qsoForm.get('myCity')?.setValue(myCity);
      this.qsoForm.get('myCounty')?.setValue(myCounty);
      this.qsoForm.get('myState')?.setValue(myState);
    }

    this.callHistory.set([]);

    this.qsoForm.markAsPristine();
    this.emitFormInit();
  }

  sendCw(text: string, k = false, updateUi = true, repeat: number|null = null, repeatDelaySeconds: number|null = null) {
    let timeToSend = 0;
    this.cqSending.set(false);
    this.cwSending.set(true);

    text = `${text}${k ? ' K' : ''}`;

    if (updateUi)
      timeToSend = Utils.calculateMorseTime(text, this.cwSpeed());

    this._infraSvc.sendCw(text, this.rigControl(), this.cwSpeed(), repeat, repeatDelaySeconds).subscribe({
      next: (r) => {
        if (r.split && !r.sent) {
          this._ntfSvc.addMessage(new NotificationMessageModel(NotificationMessageSeverity.Warn, "SPLIT ON!", "Split is on. Send again.", false));
          return;
        }
        if (updateUi)
          setTimeout(() => this.checkKeyerStatus(), timeToSend);
      },
      error: e => {
        this.cwSending.set(false);
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    });
  }

  protected sendPotaCq(startCq: boolean) {
    if (!startCq && !this.cqSending()) return;

    this.sendCw('CQ POTA DE AF0E AF0E K', false, false, 50, 5);
    this.cqSending.set(true);
  }

  stopCw() {
    this.cqSending.set(false);

    this._infraSvc.cancelCw().subscribe({
      next: () => {
        this.cwSending.set(false);
      },
      error: e => {
        this.cwSending.set(false);
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    })
  }

  protected getRadioStatus() {
    if (!this.rigControl()) return;

    this._infraSvc.getRigStatus().subscribe({
      next: r => {
        this.qsoForm.get('freq')?.setValue(r.frequencyHz, { emitEvent: false });
        this.qsoForm.get('band')?.setValue(Utils.getBandFromFrequency(r.frequencyHz), {emitEvent: false});
        this.qsoForm.get('mode')?.setValue(r.mode);
      },
      error: e => {
        this.rigControl.set(false);
        Utils.showErrorMessage(e, this._ntfSvc, this._log)
      }
    });
  }

  checkKeyerStatus() {
    this._infraSvc.getKeyerStatus().subscribe({
      next: (r) => {
        if (r.busy) {
          setTimeout(() => this.checkKeyerStatus(), 1000);
          return;
        }
        this.cwSending.set(false);
      },
      error: e => {
        this.cwSending.set(false);
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    })
  }

  protected setNoiseReduction(event: CheckboxChangeEvent) {
    this._infraSvc.setNoiseReduction(event.checked).subscribe({
      error: e => {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    })
  }

  protected setNoiseBlanket(event: CheckboxChangeEvent) {
    this._infraSvc.setNoiseBlanket(event.checked).subscribe({
      error: e => {
        Utils.showErrorMessage(e, this._ntfSvc, this._log);
      }
    })
  }

  onSave() {
    if (this.qsoForm.invalid) {
      const errors = Object.keys(this.qsoForm.controls).map(key => {
         return {
           name: key,
           errors: this.qsoForm.get(key)?.errors,
         }
      }).filter(x => x.errors != null);

      this._log.warn('Form validation failed', errors);

      this._ntfSvc.addMessage(
        new NotificationMessageModel(
          NotificationMessageSeverity.Warn,
          'Validation Error',
          'See console for details'
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

    formValue.call = formValue.call.toUpperCase();
    formValue.state = formValue.state?.toUpperCase();

    let activationId = this.potaActivation() ? this.potaActivation()!.id : 0; //stupid typescript

    if (this.isEditMode) {
      if (activationId > 0) {
        this._idbSvc.saveQso(activationId, formValue).subscribe({
          error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
        });
      }

      this._lbSvc.updateQso(formValue).subscribe({
        next: () => {
          this.saved.emit(formValue);
        },
        error: e => { Utils.showErrorMessage(e, this._ntfSvc, this._log); }
      });

      return;
    }

    let qsoCreated = false;

    if (activationId > 0) {
      this._idbSvc.saveQso(activationId, formValue).subscribe({
        error: e => Utils.showErrorMessage(e, this._ntfSvc, this._log)
      });

      qsoCreated = this.isNewActivationDay(formValue);
    }

    if (!qsoCreated)
      this.createQso(formValue, activationId, true);
  }

  private isNewActivationDay(formValue: any): boolean {
    const actDay = this.potaActivation()!.startDate.getDate(); //treats date as local, but the value is in UTC
    const qsoDate = Utils.utcStringToDate(formValue.date);

    if (actDay === qsoDate!.getDate()) return false; //same day, same activation

    const newAct:NewActivationFormData = {
      prevDayActivationId: this.potaActivation()!.id,
      parkNumber: this.potaActivation()!.parkNum,
      grid: this.potaActivation()!.grid,
      county: this.potaActivation()!.county,
      state: this.potaActivation()!.state,
      lat: this.potaActivation()!.lat ? this.potaActivation()!.lat!.toString() : '',
      lon: this.potaActivation()!.long ? this.potaActivation()!.long!.toString() : '',
      stationCallsign: this.potaActivation()!.stationCallsign,
      operatorCallsign: this.potaActivation()!.operatorCallsign,
      startDate: formValue.date
    };

    this._potaSvc.createActivation(newAct).subscribe({
      next: (id: number) => {
        this.activationCreated.emit(id);
        this.createQso(formValue, id, false);
        this._ntfSvc.addMessage(new NotificationMessageModel(NotificationMessageSeverity.Info, 'Activation Created', 'New activation day'));
      },
      error: (e) => Utils.showErrorMessage(e, this._ntfSvc, this._log)
    });

    return true;
  }

  private createQso(formValue: any, activationId: number, emitSaved: boolean) {
    this._lbSvc.createQso(activationId > 0 ? activationId : null, formValue).subscribe({
      next: (q) => {
        if (this.potaActivation()) {
          this.initializeNewQso(true);
          this.setCallFocus(false);
        }
        if (emitSaved)
          this.saved.emit(q);
      },
      error: e => { Utils.showErrorMessage(e, this._ntfSvc, this._log); }
    });
  }

  onClear(keepFreq: boolean) {
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
      this.initializeNewQso(keepFreq);
    }

    this.callHistory.set([]);
    this._lastCallsign = '';
    this.imgUrl.set('');
    this.setCallFocus(false);
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
      myCity: this.potaActivation() ? this.potaActivation()!.city : 'Broomfield',
      myCounty: this.potaActivation() ? this.potaActivation()!.county : 'Broomfield',
      myState: this.potaActivation() ? this.potaActivation()!.state : 'CO',
      myCountry: 'United States',
      myCqZone: '4',
      myItuZone: '7',
      myGrid:  this.potaActivation() ? this.potaActivation()!.grid : 'DM79lw',
      stationCallsign: this.potaActivation() ? this.potaActivation()!.stationCallsign : 'AF0E',
      operatorCallsign: this.potaActivation() ? this.potaActivation()!.operatorCallsign : 'AF0E',
      qslSent: 'N',
      qslSentDate: null,
      qslSentVia: null,
      qslRcvd: 'N',
      qslRcvdDate: null,
      qslRcvdVia: null,
      comment: this.potaActivation()?.parkNum ? `POTA activation ${this.potaActivation()!.parkNum} (${Utils.abbreviateParkName(this.potaActivation()!.parkName)})` : '',
      siteComment: '',
      radioFilter: {value: '1', disabled: true}
    };
  }

  private markAllAsTouched() {
    Object.keys(this.qsoForm.controls).forEach(key => {
      this.qsoForm.get(key)?.markAsTouched();
    });
  }

  private setCallFocus(select: boolean) {
    const callInputEl = this._callInput();

    if (callInputEl?.nativeElement) {
      setTimeout(() => {
        callInputEl.nativeElement.focus();
        if (select)
          callInputEl.nativeElement.select();
      }, 100);
    }
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

  protected onKeyDown($event: KeyboardEvent) {
    let handled = false;

    switch ($event.key) {
      case 'F1':
        if (this.editMode() !== QsoEditMode.PotaActivating) break;
        handled = true;
        $event.preventDefault();
        if (this.cqSending())
          this.stopCw();
        else
          this.sendPotaCq(true);
        break;
      case 'F2':
        handled = true;
        $event.preventDefault();
        if ($event.altKey)
          this.sendCw('R 73 de AF0E');
        else
          this.sendCw('R 73 E|E');
        break;
      case 'F3':
        handled = true;
        $event.preventDefault();
        if (this.cwCallLabel() === '???') break;

        if ($event.altKey)
          this.sendCw(this.f3CwMsg().alt, true);
        else if ($event.ctrlKey)
          this.sendCw(this.f3CwMsg().ctl, true)
        else
          this.sendCw(this.f3CwMsg().def, true)
        break;
      case 'F4':
        handled = true;
        $event.preventDefault();
        if ($event.altKey)
          this.sendCw('DE AF0|E');
        else
          this.sendCw('AF0|E');
        break;
      case 'F5':
        handled = true;
        $event.preventDefault();
        if (this.cwCallLabel() !== '???')
          this.sendCw(this.cwCallLabel()?.replaceAll('/', '//'));
        break;
      case 'F6':
        handled = true;
        $event.preventDefault();
        if (this.f3CwMsg().f6 !== '' && this.cwCallLabel() !== '???')
          this.sendCw(this.f3CwMsg().f6, true);
        break;
      case 'F8':
        handled = true;
        $event.preventDefault();
        if ($event.altKey)
          this.sendCw('AGN?');
        else
          this.sendCw('?');
        break;
      case 'F9':
        handled = true;
        this.onClear(true);
        break;
      case 'K':
      case 'k':
      case 'Л':
      case 'л':
        if ($event.ctrlKey) {
          handled = true;
          this.cwTextVisible.set(true);
        }
        break;
      case 'E':
      case 'e':
      case 'У':
      case 'у':
        if ($event.ctrlKey) {
          handled = true;
          this.sendCw('E|E', false, false);
        }
        break;
      case 'R':
      case 'r':
      case 'К':
      case 'к':
        if ($event.ctrlKey) {
          handled = true;
          this.sendCw('R R R', false, false);
        }
        break;
      case 'Escape':
        if (!this.cwSending() && !this.cqSending()) return;
        handled = true;
        this.stopCw();
        break;
      case 'Enter':
        if ($event.ctrlKey) {
          handled = true;
          this.onSave();
        }
        break;
      case 'PageDown':
        handled = true;
        let speed = this.cwSpeed();
        if (speed <= 10) return;
        this.cwSpeed.set(speed - 2);
        break;
      case 'PageUp':
        handled = true;
        let speed1 = this.cwSpeed();
        if (speed1 >= 32) return;
        this.cwSpeed.set(speed1 + 2);
        break;
      default:
        if (this.cqSending() && /^[a-z0-9]$/i.test($event.key)) //alphanumeric only
          this.stopCw();
        break;
    }

    if (handled) {
      $event.preventDefault();
      $event.stopPropagation();
    }
  }

  protected onCwKeyDown($event: KeyboardEvent) {
    const isValid = /^[a-zA-Z0-9\/]$/.test($event.key);
    if (!isValid) return;
    this.sendCw($event.key, false, false);
  }

  private emitFormInit() {
    this.formInit.emit({
      call: (this.qsoForm.get('call')?.value || '').toUpperCase(),
      qsoCount: this.callHistory().length,
      DE: {
        grid: this.qsoForm.get('myGrid')?.value || '',
        city: this.qsoForm.get('myCity')?.value || '',
        county: this.qsoForm.get('myCounty')?.value || '',
        state: this.qsoForm.get('myState')?.value || '',
      }
    });
  }
}
