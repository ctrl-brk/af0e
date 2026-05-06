import {QsoDetailModel} from './qso-detail.model';

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
