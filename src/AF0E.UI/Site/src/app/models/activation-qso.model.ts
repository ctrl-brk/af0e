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
  p2p: string[];
  satName: string | null;
  state: string | null;
}
