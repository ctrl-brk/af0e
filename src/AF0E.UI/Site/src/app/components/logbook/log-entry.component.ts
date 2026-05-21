import {Component, DestroyRef, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {QsoEditComponent, QsoEditParams} from '../qso/qso-edit.component';
import {Card} from 'primeng/card';

@Component({
  templateUrl: './log-entry.component.html',
  styleUrl: './log-entry.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    QsoEditComponent,
    Card,
  ],
})
export class LogEntryComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);

  protected title = 'QSO';
  protected readonly qsoEditParams = signal<QsoEditParams>({});

  ngOnInit(): void {
    const sub = this._activatedRoute.queryParams.subscribe( p => {
      this.qsoEditParams.set({logId: isNaN(+p['id']) ? undefined : +p['id']});
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }

  onEditModeChange(isEditMode: boolean) {
    this.title = isEditMode ? 'Edit QSO' : 'Add QSO';
  }
}
