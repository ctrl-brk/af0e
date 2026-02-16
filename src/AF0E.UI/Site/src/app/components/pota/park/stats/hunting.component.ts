import {Component, DestroyRef, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {ParkHuntingStatsComponent} from './park-hunting-stats.component';
import {Card} from 'primeng/card';

@Component({
  templateUrl: './hunting.component.html',
  styleUrl: './hunting.component.scss',
  imports: [
    ParkHuntingStatsComponent,
    Card,
  ],
})
export class PotaParkHuntingComponent implements OnInit {
  private _activatedRoute = inject(ActivatedRoute);
  private _destroyRef = inject(DestroyRef);

  protected parkNum = signal('');

  ngOnInit(): void {
    const sub = this._activatedRoute.paramMap.subscribe({
      next: (x) => {
        this.parkNum.set(x.get('parkNum')!);
      }
    });

    this._destroyRef.onDestroy(() => sub.unsubscribe());
  }
}
