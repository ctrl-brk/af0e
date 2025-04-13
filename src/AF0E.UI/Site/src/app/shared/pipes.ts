import {Pipe, PipeTransform} from '@angular/core';

@Pipe({
  name: 'modeSeverity'
})
export class ModeSeverityPipe implements PipeTransform {
  transform(mode: string) {
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
      case 'MFSK':
      case 'PSK31':
      case 'JT65':
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
