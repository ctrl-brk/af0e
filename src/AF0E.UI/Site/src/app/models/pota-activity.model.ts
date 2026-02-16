export interface PotaActivityModel {
  callSign: string;
  active: boolean
  parkNum: string | null;
  freqKhz: string | null;
  freqHz: number | null;
  band: string | null;
  mode: string | null;
  lastSpotTime: Date | null;
}
