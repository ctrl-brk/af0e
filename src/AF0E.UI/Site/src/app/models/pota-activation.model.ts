export interface PotaActivationModel {
  id: number;
  startDate: Date;
  endDate: Date;
  logSubmittedDate: Date;
  parkNum: string;
  parkName: string;
  siteComments: string;
  city: string;
  county: string;
  state: string;
  lat: number | null;
  long: number | null;
  count: number;
  cwCount: number;
  digiCount: number;
  phoneCount: number;
}
