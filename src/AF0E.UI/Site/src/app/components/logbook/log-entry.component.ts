import {Component, DestroyRef, inject, OnInit, ViewEncapsulation} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {Card} from 'primeng/card';
import {QsoDetailModel} from '../../models/qso-detail.model';
import {QsoEditComponent} from './qso-edit.component';

@Component({
  templateUrl: './log-entry.component.html',
  styleUrl: './log-entry.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    Card,
    QsoEditComponent,
  ],
})
export class LogEntryComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute)
  private _destroyRef = inject(DestroyRef);

  protected logId = 0;
  protected qso: QsoDetailModel = null!;

  ngOnInit(): void {
    const sub = this._activatedRoute.queryParams.subscribe( p => {
      this.logId = isNaN(+p['id']) ? 0 : +p['id'];
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }
}
