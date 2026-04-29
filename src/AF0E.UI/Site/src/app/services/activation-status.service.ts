import {Injectable, signal} from '@angular/core';

export type ActivationStatus = 'none' | 'in-progress' | 'qualified';

@Injectable({providedIn: 'root'})
export class ActivationStatusService {
  /** Set by the activation component, read by the header */
  readonly status = signal<ActivationStatus>('none');

  set(qsoCount: number | null) {
    if (qsoCount === null) {
      this.status.set('none');
    } else if (qsoCount >= 10) {
      this.status.set('qualified');
    } else {
      this.status.set('in-progress');
    }
  }

  clear() {
    this.status.set('none');
  }
}

