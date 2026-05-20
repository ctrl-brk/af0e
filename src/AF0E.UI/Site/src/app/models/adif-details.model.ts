import {Utils} from '../shared/utils';
import {adifDate, adifField, adifMapMode, adifTime} from '../shared/adif.utils';

export interface AdifDetailsModel {
  date: Date;
  call: string;
  band: string;
  bandRx: string;
  freqHz: number;
  freqRxHz: number;
  mode: string;
  rstSent: string | null;
  rstRcvd: string | null;
  myCounty: string;
  myState: string;
  myCountry: string;
  myGrid: string;
  stationCallsign: string | null;
  operatorCallsign: string | null;
  satName: string;
  satMode: string;
}

export function adifDetailsToAdifString(qso: AdifDetailsModel): string {
  const d = new Date(qso.date);

  let rec = '';
  rec += adifField('QSO_DATE', adifDate(d));
  rec += adifField('TIME_ON', adifTime(d));
  rec += adifField('CALL', qso.call);
  rec += adifField('BAND', qso.band);
  rec += adifField('MODE', adifMapMode(qso.mode));
  rec += adifField('FREQ', qso.freqHz ? (qso.freqHz / 1_000_000).toFixed(6) : null);

  if (qso.bandRx && qso.bandRx !== qso.band) rec += adifField('BAND_RX', qso.bandRx);
  if (qso.freqRxHz && qso.freqRxHz !== qso.freqHz) rec += adifField('FREQ_RX', qso.freqRxHz ? (qso.freqRxHz / 1_000_000).toFixed(6) : null);

  rec += adifField('MY_CNTY', qso.myCounty || 'Broomfield');
  rec += adifField('MY_GRIDSQUARE', qso.myGrid || 'DM79lw');
  rec += adifField('MY_STATE', qso.myState || 'CO');

  if (qso.satName) {
    rec += adifField('SAT_NAME', qso.satName);
    rec += adifField('PROP_MODE', 'SAT');
  }

  rec += adifField('STATION_CALLSIGN', qso.stationCallsign || Utils.getMyEffectiveCall(qso.date, false));
  rec += adifField('OPERATOR_CALLSIGN', qso.operatorCallsign || Utils.getMyEffectiveCall(qso.date, false));

  rec += '<EOR>\n';
  return rec;
}

export function adifDetailsToAdifFile(entries: AdifDetailsModel[], options?: { call?: string | null; from?: Date | null; to?: Date | null }): { content: string; filename: string } {
  const from = options?.from ? new Date(options.from) : null;
  const to = options?.to ? new Date(options.to) : null;
  const fromStr = from ? `${from.getFullYear()}-${String(from.getMonth() + 1).padStart(2, '0')}-${String(from.getDate()).padStart(2, '0')}` : 'all';
  const toStr = to ? `${to.getFullYear()}-${String(to.getMonth() + 1).padStart(2, '0')}-${String(to.getDate()).padStart(2, '0')}` : fromStr;
  const subject = options?.call ? options.call : 'AF0E logbook';

  const header =
    `ADIF for ${subject} from ${fromStr} to ${toStr}\n` +
    `<ADIF_VER:5>3.1.5\n` +
    `<PROGRAMID:8>AF0E.org\n` +
    `<EOH>\n`;

  return {
    content: header + entries.map(qso => adifDetailsToAdifString(qso)).join(''),
    filename: `${fromStr}${fromStr === toStr ? '' : ` to ${toStr}`}${options?.call ? ` ${options.call.replaceAll('/', '_')}` : ''}.adi`,
  };
}
