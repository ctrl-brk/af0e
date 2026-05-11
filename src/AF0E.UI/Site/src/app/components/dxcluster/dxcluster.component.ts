import {DatePipe, DecimalPipe, NgClass} from '@angular/common';
import {Component, computed, DestroyRef, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {Select} from 'primeng/select';
import {TableModule} from 'primeng/table';
import {Tag} from 'primeng/tag';
import {DxClusterFilterModel, DxClusterFrequencyWindowModel} from '../../models/dx-cluster-filter.model';
import {DxClusterSpotModel} from '../../models/dx-cluster-spot.model';
import {DxClusterStatusModel} from '../../models/dx-cluster-status.model';
import {DxClusterService} from '../../services/dx-cluster.service';
import {LogService} from '../../shared/log.service';
import {NotificationService} from '../../shared/notification.service';
import {Utils} from '../../shared/utils';
import {Fieldset} from 'primeng/fieldset';

interface DxClusterFilterOption {
  label: string;
  value: string | null;
}

@Component({
  selector: 'app-dxcluster',
  templateUrl: './dxcluster.component.html',
  styleUrl: './dxcluster.component.scss',
  encapsulation: ViewEncapsulation.None,
  imports: [
    TableModule,
    Button,
    Select,
    Tag,
    FormsModule,
    DatePipe,
    DecimalPipe,
    NgClass,
    Fieldset,
  ]
})
export class DxClusterComponent implements OnInit {
  private static readonly SelectedFilterStorageKey = 'dxcluster.selectedFilter';
  private static readonly FrequencyFormatter = new Intl.NumberFormat(undefined, {maximumFractionDigits: 3});

  private readonly _destroyRef = inject(DestroyRef);
  private readonly _dxClusterSvc = inject(DxClusterService);
  private readonly _ntfSvc = inject(NotificationService);
  private readonly _log = inject(LogService);
  private readonly _callsignRegexCache = new Map<string, RegExp[]>();

  protected readonly spots = signal<DxClusterSpotModel[]>([]);
  protected readonly status = signal<DxClusterStatusModel | null>(null);
  protected readonly loading = signal(false);
  protected readonly statusLoading = signal(false);
  protected readonly selectedFilterName = signal<string | null>(this.readSelectedFilterName());

  protected readonly primaryServer = computed(() => this.status()?.servers?.[0] ?? null);
  protected readonly isConnected = computed(() => this.primaryServer()?.connected ?? false);
  protected readonly selectedFilter = computed(() => {
    const filterName = this.selectedFilterName();
    if (!filterName)
      return null;

    return this.status()?.filters.find(filter => filter.name.localeCompare(filterName, undefined, {sensitivity: 'accent'}) === 0) ?? null;
  });
  protected readonly filterOptions = computed<DxClusterFilterOption[]>(() => [
    {label: 'All spots', value: null},
    ...((this.status()?.filters ?? []).map(filter => ({label: filter.name, value: filter.name}))),
  ]);
  protected readonly visibleSpots = computed(() => {
    const filter = this.selectedFilter();
    if (!filter)
      return this.spots();

    return this.spots().filter(spot => this.matchesFilter(filter, spot));
  });
  protected readonly selectedFilterSummary = computed(() => {
    const filter = this.selectedFilter();
    if (!filter)
      return null;

    const parts: string[] = [];
    const callsignPatterns = filter.callsignPatterns ?? '';
    const modes = filter.modes ?? [];
    const frequencyWindows = filter.frequencyWindows ?? [];

    if (callsignPatterns.trim())
      parts.push(`DX: ${callsignPatterns}`);

    if (modes.length > 0)
      parts.push(`Modes: ${modes.join(', ')}`);

    if (frequencyWindows.length > 0)
      parts.push(`Freq: ${frequencyWindows.map(window => this.describeFrequencyWindow(window)).join(', ')}`);

    if (filter.invalidCallsignPatterns.length > 0)
      parts.push(`Ignored invalid regex: ${filter.invalidCallsignPatterns.join(', ')}`);

    return parts.join(' • ');
  });
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
      .subscribe(status => this.applyStatus(status));

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
      this.applyStatus(status);
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

  protected onFilterChanged(filterName: string | null): void {
    this.selectedFilterName.set(filterName || null);
    this.persistSelectedFilterName(filterName || null);
  }


  private loadSnapshot(): void {
    this.statusLoading.set(true);
    this.loading.set(true);

    this._dxClusterSvc.getStatus()
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: status => {
          this.applyStatus(status);
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

  private applyStatus(status: DxClusterStatusModel): void {
    this.status.set(status);

    const selectedFilterName = this.selectedFilterName();
    if (!selectedFilterName)
      return;

    const exists = status.filters.some(filter => filter.name.localeCompare(selectedFilterName, undefined, {sensitivity: 'accent'}) === 0);
    if (exists)
      return;

    this.onFilterChanged(null);
  }

  private matchesFilter(filter: DxClusterFilterModel, spot: DxClusterSpotModel): boolean {
    const callsignPatterns = filter.callsignPatterns ?? '';
    const frequencyWindows = filter.frequencyWindows ?? [];
    const modes = filter.modes ?? [];

    const regexes = this.getCallsignRegexes(callsignPatterns);
    if (regexes.length > 0 && !regexes.some(regex => regex.test(spot.dxCallsign)))
      return false;

    if (frequencyWindows.length > 0 && !frequencyWindows.some(window => this.matchesFrequencyWindow(window, spot.frequencyKhz)))
      return false;

    if (modes.length > 0) {
      const normalizedSpotMode = this.normalizeMode(spot.mode);
      if (!normalizedSpotMode)
        return false;

      const normalizedFilterModes = modes.map(mode => this.normalizeMode(mode)).filter((mode): mode is string => !!mode);
      if (!normalizedFilterModes.includes(normalizedSpotMode))
        return false;
    }

    return true;
  }

  private getCallsignRegexes(patterns: string): RegExp[] {
    const cacheKey = patterns.trim();
    if (!cacheKey)
      return [];

    const cached = this._callsignRegexCache.get(cacheKey);
    if (cached)
      return cached;

    const regexes = cacheKey
      .split('|')
      .map(pattern => pattern.trim())
      .filter(pattern => pattern.length > 0)
      .map((pattern) => {
        try {
          return new RegExp(`^(?:${pattern})$`, 'i');
        } catch (e) {
          this._log.warn(`Invalid DX cluster filter regex ignored: ${pattern}`, e);
          return null;
        }
      })
      .filter((regex): regex is RegExp => regex !== null);

    this._callsignRegexCache.set(cacheKey, regexes);
    return regexes;
  }

  private matchesFrequencyWindow(window: DxClusterFrequencyWindowModel, frequencyKhz: number): boolean {
    if (window.minFrequencyKhz !== null && frequencyKhz < window.minFrequencyKhz)
      return false;

    return !(window.maxFrequencyKhz !== null && frequencyKhz > window.maxFrequencyKhz);
  }

  private describeFrequencyWindow(window: DxClusterFrequencyWindowModel): string {
    const min = window.minFrequencyKhz === null ? 'any' : `${DxClusterComponent.FrequencyFormatter.format(window.minFrequencyKhz)} kHz`;
    const max = window.maxFrequencyKhz === null ? 'any' : `${DxClusterComponent.FrequencyFormatter.format(window.maxFrequencyKhz)} kHz`;

    if (window.minFrequencyKhz !== null && window.maxFrequencyKhz !== null)
      return `${min}–${max}`;

    if (window.minFrequencyKhz !== null)
      return `${min}+`;

    return `≤ ${max}`;
  }

  private normalizeMode(mode: string | null): string | null {
    if (!mode)
      return null;

    switch (mode.trim().toUpperCase()) {
      case 'USB':
      case 'LSB':
      case 'SSB':
      case 'PHONE':
        return 'SSB';
      case 'DIGI':
      case 'DIGITAL':
      case 'DATA':
        return 'DIGI';
      default:
        return mode.trim().toUpperCase();
    }
  }

  private readSelectedFilterName(): string | null {
    try {
      return localStorage.getItem(DxClusterComponent.SelectedFilterStorageKey);
    } catch {
      return null;
    }
  }

  private persistSelectedFilterName(filterName: string | null): void {
    try {
      if (filterName)
        localStorage.setItem(DxClusterComponent.SelectedFilterStorageKey, filterName);
      else
        localStorage.removeItem(DxClusterComponent.SelectedFilterStorageKey);
    } catch {
    }
  }
}
