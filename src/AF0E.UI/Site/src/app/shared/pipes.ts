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
