import {DatePipe, DecimalPipe, NgClass} from '@angular/common';
import {Component, computed, DestroyRef, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {Button} from 'primeng/button';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {DxClusterSpotModel} from '../../models/dx-cluster-spot.model';
import {DxClusterStatusModel} from '../../models/dx-cluster-status.model';
import {DxClusterService} from '../../services/dx-cluster.service';
import {LogService} from '../../shared/log.service';
import {NotificationService} from '../../shared/notification.service';
import {Utils} from '../../shared/utils';

@Component({
  selector: 'app-dxcluster',
  templateUrl: './dxcluster.component.html',
  styleUrl: './dxcluster.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    TableModule,
    Button,
    Tag,
    DatePipe,
    DecimalPipe,
    NgClass,
  ]
})
export class DxClusterComponent implements OnInit {
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _dxClusterSvc = inject(DxClusterService);
  private readonly _ntfSvc = inject(NotificationService);
  private readonly _log = inject(LogService);

  protected readonly spots = signal<DxClusterSpotModel[]>([]);
  protected readonly status = signal<DxClusterStatusModel | null>(null);
  protected readonly loading = signal(false);
  protected readonly statusLoading = signal(false);

  protected readonly primaryServer = computed(() => this.status()?.servers?.[0] ?? null);
  protected readonly isConnected = computed(() => this.primaryServer()?.connected ?? false);
  protected readonly statusSeverity = computed<'success' | 'warn' | 'secondary'>(() => {
    const status = this.status();
    if (!status?.configured)
      return 'secondary';

    return this.isConnected() ? 'success' : 'warn';
  });

  async ngOnInit(): Promise<void> {
    this.loadSnapshot();

    this._dxClusterSvc.status$
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(status => this.status.set(status));

    this._dxClusterSvc.spot$
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe(spot => {
        this.spots.update(current => {
          const exists = current.some(existing => this.getSpotKey(existing) === this.getSpotKey(spot));
          if (exists)
            return current;

          return [spot, ...current]
            .sort((a, b) => b.spotTimeUtc.getTime() - a.spotTimeUtc.getTime())
            .slice(0, 250);
        });
      });

    try {
      const status = await this._dxClusterSvc.subscribeDxCluster();
      this.status.set(status);
    } catch (e) {
      Utils.showErrorMessage(e, this._ntfSvc, this._log);
    }

    this._destroyRef.onDestroy(() => {
      void this._dxClusterSvc.unsubscribeDxCluster();
    });
  }

  protected refresh(): void {
    this.loadSnapshot();
  }

  protected trackSpot(index: number, spot: DxClusterSpotModel): string {
    return this.getSpotKey(spot);
  }

  private loadSnapshot(): void {
    this.statusLoading.set(true);
    this.loading.set(true);

    this._dxClusterSvc.getStatus()
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: status => {
          this.status.set(status);
          this.statusLoading.set(false);
        },
        error: e => {
          this.statusLoading.set(false);
          Utils.showErrorMessage(e, this._ntfSvc, this._log);
        }
      });

    this._dxClusterSvc.getSpots()
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: spots => {
          this.spots.set([...spots].sort((a, b) => b.spotTimeUtc.getTime() - a.spotTimeUtc.getTime()));
          this.loading.set(false);
        },
        error: e => {
          this.loading.set(false);
          Utils.showErrorMessage(e, this._ntfSvc, this._log);
        }
      });
  }

  private getSpotKey(spot: DxClusterSpotModel): string {
    return [
      spot.sourceName,
      spot.dxCallsign,
      spot.spotterCallsign,
      spot.frequencyKhz,
      spot.spotTimeUtc.toISOString(),
    ].join('|');
  }
}
