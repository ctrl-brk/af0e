import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
  name: 'modeSeverity'
})
export class ModeSeverityPipe implements PipeTransform {
  transform(mode: string) {
    if (mode)
      mode = mode.toUpperCase();

    switch (mode) {
      case 'CW':
        return 'success';

      case 'SSB':
      case 'LSB':
      case 'USB':
      case 'FM':
      case 'AM':
        return 'info';

      case 'FT8':
      case 'FT4':
      case 'MFSK':
      case 'PSK31':
      case 'JT65':
      case 'RTTY':
      case 'SSTV':
        return 'warn';
    }
    return 'secondary';
  }
}

@Pipe({
  name: 'qsoMode'
})
export class QsoModePipe implements PipeTransform {
  transform(mode: string) {
    if (mode)
      mode = mode.toUpperCase();

    switch (mode) {
      case 'USB':
      case 'LSB':
        return 'SSB';
      case 'MFSK':
        return 'FT4';
    }
    return mode;
  }
}

@Pipe({
  name: 'grid'
})
export class GridPipe implements PipeTransform {
  transform(grid: string) {
    if (!grid) return '';

    return grid.length === 4 ? grid.toUpperCase() : grid.slice(0, 4).toUpperCase() + grid.slice(4).toLowerCase();
  }
}

@Pipe({
  name: 'timeAgo'
})
export class TimeAgoPipe implements PipeTransform {
  transform(value: Date | null): string {
    if (!value) return '';

    const nowUtc = Date.now(); // Current time in UTC milliseconds
    const spotTime = new Date(value).getTime();
    const diffMs = nowUtc - spotTime;
    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffSeconds / 60);
    const remainingSeconds = diffSeconds % 60;

    if (diffMinutes < 1) {
      // Less than a minute: show seconds only
      return `${diffSeconds}s`;
    } else if (diffMinutes < 5) {
      // Less than 5 minutes: show minutes and seconds
      return `${diffMinutes}m ${remainingSeconds}s`;
    } else {
      // 5 minutes or more: show minutes only
      return `${diffMinutes}m`;
    }
  }
}

