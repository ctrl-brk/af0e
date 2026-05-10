import {QsoDetailModel} from './qso-detail.model';
import {PotaActivationModel} from './pota-activation.model';
import {Utils} from '../shared/utils';
import {adifDate, adifField, adifMapMode, adifTime} from '../shared/adif.utils';

export interface ActivationQsoModel {
  logId: number;
  band: string | null;
  call: string;
  cqz: number | null;
  date: Date;
  dxcc: string | null;
  freq: number | null;
  grid: string | null;
  ituz: number | null;
  lon: number | null;
  lat: number | null;
  myCity: string | null;
  myCountry: string | null;
  myCnty: string | null;
  myGrid: string | null;
  myState: string | null;
  rstRcvd: string | null;
  rstSent: string | null;
  mode: string | null;
  stationCallsign: string | null;
  operatorCallsign: string | null;
  p2p: string[];
  satName: string | null;
  state: string | null;
}

/**
 * Converts a QsoDetailModel to an ActivationQsoModel, only populating grid fields
 */
export function QsoDetailsToActivationQsoModel(q: QsoDetailModel): ActivationQsoModel {
  return {
    cqz: null,
    dxcc: null,
    freq: null,
    grid: null,
    ituz: null,
    lat: null,
    lon: null,
    myCity: null,
    myCnty: null,
    myCountry: null,
    myGrid: null,
    myState: null,
    p2p: [],
    rstRcvd: null,
    rstSent: null,
    state: null,
    stationCallsign: null,
    operatorCallsign: null,
    logId: q.id,
    date: q.date,
    call: q.call,
    band: q.band,
    mode: q.mode,
    satName: q.satName
  };
}

/**
 * Converts a QsoDetailModel to an ActivationQsoModel, only populating grid fields
 * @param qso  The QSO to convert.
 * @param activation  The activation context (park, location fields, etc.).
 */
export function activationQsoToAdif(qso: ActivationQsoModel, activation: PotaActivationModel): string {
  const d = new Date(qso.date);

  let rec = '';
  rec += adifField('BAND', qso.band);
  rec += adifField('CALL', qso.call);
  rec += adifField('CQZ', qso.cqz);
  rec += adifField('DXCC', qso.dxcc);
  rec += adifField('FREQ', qso.freq ? (qso.freq / 1_000_000).toFixed(6) : null);
  rec += adifField('GRIDSQUARE', qso.grid);
  rec += adifField('ITUZ', qso.ituz);
  rec += adifField('MODE', adifMapMode(qso.mode));
  rec += adifField('MY_CITY', qso.myCity);
  if (activation.county) rec += adifField('MY_CNTY', qso.myCnty);
  rec += adifField('MY_COUNTRY', qso.myCountry);
  if (activation.grid) rec += adifField('MY_GRIDSQUARE', qso.myGrid);
  rec += adifField('MY_POTA_REF', activation.parkNum);
  rec += adifField('MY_SIG', 'POTA');
  rec += adifField('MY_SIG_INFO', activation.parkNum);
  if (activation.state) rec += adifField('MY_STATE', qso.myState);
  rec += adifField('OPERATOR', qso.operatorCallsign ?? Utils.getMyEffectiveCall(qso.date));
  rec += adifField('QSLMSG', `POTA ${activation.parkNum}`);
  rec += adifField('QSO_DATE', adifDate(d));
  rec += adifField('RST_RCVD', qso.rstRcvd);
  rec += adifField('RST_SENT', qso.rstSent);
  rec += adifField('STATE', qso.state);
  rec += adifField('STATION_CALLSIGN', qso.stationCallsign ?? Utils.getMyEffectiveCall(qso.date));
  rec += adifField('TIME_ON', adifTime(d));
  rec += '<EOR>\n';
  return rec;
}

/**
 * Builds a complete ADIF file string (header + all QSO records) for an activation.
 * @param entries  The log entries to export.
 * @param activation  The activation context.
 * @returns  The full ADIF file content and a suggested filename.
 */
export function activationLogToAdif(entries: ActivationQsoModel[], activation: PotaActivationModel): { content: string; filename: string } {
  const sd = activation.startDate;
  const dateStr =
    `${sd.getFullYear()}-` +
    `${String(sd.getMonth() + 1).padStart(2, '0')}-` +
    `${String(sd.getDate()).padStart(2, '0')}`;

  const header =
    `ADIF for ${activation.stationCallsign}: POTA at ${activation.parkNum} ${activation.parkName} on ${dateStr}\n` +
    `<ADIF_VER:5>3.1.5\n` +
    `<PROGRAMID:8>AF0E.org\n` +
    `<EOH>\n`;

  const records = entries.map(qso => activationQsoToAdif(qso, activation)).join('');

  return {
    content: header + records,
    filename: `${dateStr} ${activation.parkNum} (${Utils.abbreviateParkName(activation.parkName)}).adi`,
  };
}
