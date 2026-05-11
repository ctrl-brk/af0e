/** Returns a single ADIF field token, e.g. `<CALL:5>W1AW`, or '' if value is falsy. */
export function adifField(name: string, value: string | number | null | undefined): string {
  if (!value) return '';
  if (typeof value === 'number') value = value.toString();
  return `<${name}:${value.length}>${value}`;
}

/** Normalizes a mode string to its ADIF equivalent. */
export function adifMapMode(mode: string | null | undefined): string | null {
  if (!mode) return null;
  const m = mode.toUpperCase();
  if (m === 'USB' || m === 'LSB') return 'SSB';
  if (m === 'MFSK') return 'FT4';
  return m;
}

/** Formats a Date as an ADIF QSO_DATE string (YYYYMMDD). */
export function adifDate(d: Date): string {
  return `${d.getFullYear()}${String(d.getMonth() + 1).padStart(2, '0')}${String(d.getDate()).padStart(2, '0')}`;
}

/** Formats a Date as an ADIF TIME_ON string (HHMMSS). */
export function adifTime(d: Date): string {
  return `${String(d.getHours()).padStart(2, '0')}${String(d.getMinutes()).padStart(2, '0')}${String(d.getSeconds()).padStart(2, '0')}`;
}
