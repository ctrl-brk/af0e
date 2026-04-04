export interface PotaActivationModel {
  id: number;
  startDate: Date;
  endDate: Date | null;
  logSubmittedDate: Date | null;
  parkNum: string;
  parkName: string;
  siteComments: string;
  city: string;
  county: string;
  state: string;
  grid: string;
  lat: number | null;
  long: number | null;
  count: number;
  cwCount: number;
  digiCount: number;
  phoneCount: number;
  p2pCount: number;
  status: string;
}
