export interface QsoSummaryModel {
  totalCount: number;
  id: number;
  date: Date;
  call: string;
  mode: string;
  band: string;
  satName: string;
  potaCount: number;
}
