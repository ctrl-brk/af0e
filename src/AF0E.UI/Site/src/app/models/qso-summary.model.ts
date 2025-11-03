export interface QsoSummaryModel {
  id: number;
  date: Date;
  call: string;
  mode: string;
  band: string;
  grid: string;
  satName: string;
  potaCount: number;
  metadata: string;
}
