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
  rstSent: number | null;
  rstRcvd: number | null;
  myCity: string;
  myCounty: string;
  myState: string;
  myCountry: string;
  myCqZone: string;
  myItuZone: string;
  myGrid: string;
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
