export interface QsoSummaryModel {
  id: number;
  date: Date;
  call: string;
  mode: string;
  band: string;
  stationCallsign: string;
  operatorCallsign: string;
  comment: string;
  grid: string;
  satName: string;
  potaCount: number;
  metadata: string;
}
