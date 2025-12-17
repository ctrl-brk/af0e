export interface DropdownOption {
  label: string;
  value: string;
}

export const MODE_OPTIONS: DropdownOption[] = [
  { label: 'FT8', value: 'FT8' },
  { label: 'CW', value: 'CW' },
  { label: 'SSB', value: 'SSB' },
  { label: 'FM', value: 'FM' },
  { label: 'FT4', value: 'FT4' },
  { label: 'MFSK', value: 'MFSK' },
  { label: 'PSK31', value: 'PSK31' },
  { label: 'JT65', value: 'JT65' },
  { label: 'USB', value: 'USB' },
  { label: 'LSB', value: 'LSB' },
  { label: 'AM', value: 'AM' },
];

export const BAND_OPTIONS: DropdownOption[] = [
  { label: '160m', value: '160m' },
  { label: '80m', value: '80m' },
  { label: '60m', value: '60m' },
  { label: '40m', value: '40m' },
  { label: '30m', value: '30m' },
  { label: '20m', value: '20m' },
  { label: '17m', value: '17m' },
  { label: '15m', value: '15m' },
  { label: '12m', value: '12m' },
  { label: '10m', value: '10m' },
  { label: '6m', value: '6m' },
  { label: '2m', value: '2m' },
  { label: '70cm', value: '70cm' },
];

export const QSL_OPTIONS: DropdownOption[] = [
  { label: 'No', value: 'N' },
  { label: 'Verified', value: 'V' },
  { label: 'Queued', value: 'Q' },
  { label: 'Requested', value: 'R' },
  { label: 'Yes', value: 'Y' },
  { label: 'Ignore', value: 'I' },
];

export const QSL_VIA_OPTIONS: DropdownOption[] = [
  { label: 'Bureau', value: 'B' },
  { label: 'Direct', value: 'D' },
  { label: 'Electronic', value: 'E' },
  { label: 'Manager', value: 'M' },
];
