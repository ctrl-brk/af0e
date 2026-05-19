import {DatePipe, DecimalPipe, NgClass, NgOptimizedImage} from '@angular/common';
import {Component, computed, DestroyRef, inject, OnInit, output, signal, ViewEncapsulation} from '@angular/core';
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
import {ModeSeverityPipe, QsoModePipe} from '../../shared/pipes';

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
    ModeSeverityPipe,
    QsoModePipe,
    NgOptimizedImage,
  ]
})
export class DxClusterComponent implements OnInit {
  private static readonly SelectedFilterStorageKey = 'dxcluster.selectedFilter';
  private static readonly FrequencyFormatter = new Intl.NumberFormat(undefined, {maximumFractionDigits: 3});
  private static readonly FlagCdnBaseUrl = 'https://flagcdn.com';
  private static readonly TimeOnlyCommentRegex = /^\d{3,4}Z$/i;
  private static readonly PotaTagRegex = /(?:_pota_|\bpota\b)/i;
  private static readonly PotaTagReplaceRegex = /(?:_pota_|\bpota\b)/gi;
  private static readonly PotaCommentPrefix = '🌲';
  private static readonly DigitalModes = new Set([
    'DIGI',
    'FT8',
    'FT4',
    'FT2',
    'JS8',
    'MFSK',
    'MSK144',
    'RTTY',
    'PSK',
    'JT65',
    'JT9',
    'SSTV',
    'WSPR',
    'OLIVIA',
    'DOMINO',
    'THOR',
    'HELL',
    'PACKET',
    'PKT',
  ]);

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
  tune = output<DxClusterSpotModel>();

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

  protected getDxccFlagUrl(spot: DxClusterSpotModel): string | null {
    const countryCode = spot.dxccCountryCode?.trim().toLowerCase();
    if (!countryCode || !/^[a-z]{2}$/.test(countryCode))
      return null;

    return `${DxClusterComponent.FlagCdnBaseUrl}/16x12/${countryCode}.png`;
  }

  protected getSpotRowClasses(spot: DxClusterSpotModel): Record<string, boolean> {
    return {
      'dxcluster-row-offline': !this.isConnected(),
      'dxcluster-row-verified-band-mode': spot.dxccWorkedStatus === 'VerifiedBandMode',
      'dxcluster-row-verified-other': spot.dxccWorkedStatus === 'VerifiedOtherBandMode',
      'dxcluster-row-worked-unverified': spot.dxccWorkedStatus === 'WorkedNotVerified',
      'dxcluster-row-not-worked': spot.dxccWorkedStatus === 'NotWorked',
    };
  }

  protected getDisplayComment(spot: DxClusterSpotModel): string | null {
    const comment = spot.comment?.trim() ?? '';
    if (!comment)
      return spot.rawLine;

    const hasPotaTag = DxClusterComponent.PotaTagRegex.test(comment);
    const commentWithoutPotaTag = comment
      .replace(DxClusterComponent.PotaTagReplaceRegex, ' ')
      .replace(/\s+/g, ' ')
      .trim();

    if (hasPotaTag) {
      if (!commentWithoutPotaTag || DxClusterComponent.TimeOnlyCommentRegex.test(commentWithoutPotaTag))
        return DxClusterComponent.PotaCommentPrefix;

      return `${DxClusterComponent.PotaCommentPrefix} ${commentWithoutPotaTag}`;
    }

    return DxClusterComponent.TimeOnlyCommentRegex.test(comment) ? null : comment;
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
      if (!modes.some(mode => this.modesMatch(mode, spot.mode)))
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

    const normalized = mode.trim().toUpperCase();

    switch (normalized) {
      case 'USB':
      case 'LSB':
      case 'SSB':
      case 'PHONE':
        return 'SSB';
      case 'DIGI':
      case 'DIGITAL':
      case 'DATA':
        return 'DIGI';
      case 'FT-8':
        return 'FT8';
      case 'FT-4':
        return 'FT4';
      case 'FT-2':
        return 'FT2';
      case 'JS8CALL':
        return 'JS8';
      case 'FSK':
        return 'RTTY';
      default:
        if (normalized.startsWith('MFSK'))
          return 'MFSK';

        if (normalized.startsWith('PSK'))
          return 'PSK';

        return normalized;
    }
  }

  private modesMatch(left: string | null | undefined, right: string | null | undefined): boolean {
    const normalizedLeft = this.normalizeMode(left ?? null);
    const normalizedRight = this.normalizeMode(right ?? null);
    if (!normalizedLeft || !normalizedRight)
      return false;

    if (normalizedLeft === normalizedRight)
      return true;

    return (normalizedLeft === 'DIGI' && this.isDigitalMode(normalizedRight))
      || (normalizedRight === 'DIGI' && this.isDigitalMode(normalizedLeft));
  }

  private isDigitalMode(mode: string): boolean {
    return DxClusterComponent.DigitalModes.has(mode);
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

  protected onFreqClicked(spot: DxClusterSpotModel) {
    this.tune.emit(spot);
  }
}
