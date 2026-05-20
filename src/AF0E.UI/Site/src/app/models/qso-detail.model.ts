import {Utils} from '../shared/utils';
import {adifDate, adifField, adifMapMode, adifTime} from '../shared/adif.utils';

export interface QsoDetailModel {
  totalCount: number;
  id: number;
  date: Date;
  call: string;
  band: string;
  bandRx: string;
  freq: number;
  freqRx: number;
  mode: string;
  rstSent: string | null;
  rstRcvd: string | null;
  name_fmt: string | null;
  county: string | null;
  state: string | null;
  country: string | null;
  grid: string | null;
  cqZone: number | null;
  ituZone: number | null;
  dxcc: number | null;
  myCity: string;
  myCounty: string;
  myState: string;
  myCountry: string;
  myCqZone: string;
  myItuZone: string;
  myGrid: string;
  stationCallsign: string | null;
  operatorCallsign: string | null;
  qslSent: string,
  qslSentDate: Date | null,
  qslSentVia: string | null,
  qslRcvd: string,
  qslRcvdDate: Date | null,
  qslRcvdVia: string | null,
  pota: string[],
  p2p: boolean;
  satName: string;
  satMode: string;
  contest: string;
  siteComment: string;
  comment: string;
}

export function qsoDetailToAdif(qso: QsoDetailModel): string {
  const d = new Date(qso.date);

  let rec = '';
  rec += adifField('BAND', qso.band);
  rec += adifField('CALL', qso.call);
  rec += adifField('CNTY', qso.county);
  rec += adifField('COMMENT', qso.comment);
  rec += adifField('CQZ', qso.cqZone);
  rec += adifField('DXCC', qso.dxcc);
  rec += adifField('FREQ', qso.freq ? (qso.freq / 1_000_000).toFixed(6) : null);
  rec += adifField('GRIDSQUARE', qso.grid);
  rec += adifField('ITUZ', qso.ituZone);
  rec += adifField('MODE', adifMapMode(qso.mode));
  rec += adifField('MY_CITY', qso.myCity);
  rec += adifField('MY_CNTY', qso.myCounty);
  rec += adifField('MY_COUNTRY', qso.myCountry);
  rec += adifField('MY_GRIDSQUARE', qso.myGrid);
  rec += adifField('MY_STATE', qso.myState);
  if (qso.pota?.length) {
    const potaStr = qso.pota.join(',');
    rec += adifField('MY_POTA_REF', potaStr);
    rec += adifField('MY_SIG', 'POTA');
    rec += adifField('MY_SIG_INFO', potaStr);
  }
  rec += adifField('NAME', qso.name_fmt);
  rec += adifField('OPERATOR', qso.operatorCallsign ?? Utils.getMyEffectiveCall(qso.date, false));
  rec += adifField('QSO_DATE', adifDate(d));
  rec += adifField('QSL_RCVD', qso.qslRcvd);
  rec += adifField('QSL_SENT', qso.qslSent);
  rec += adifField('RST_RCVD', qso.rstRcvd);
  rec += adifField('RST_SENT', qso.rstSent);
  if (qso.satName) {
    rec += adifField('SAT_NAME', qso.satName);
    rec += adifField('PROP_MODE', 'SAT');
  }
  rec += adifField('STATE', qso.state);
  rec += adifField('STATION_CALLSIGN', qso.stationCallsign ?? Utils.getMyEffectiveCall(qso.date, false));
  rec += adifField('TIME_ON', adifTime(d));
  rec += '<EOR>\n';
  return rec;
}

