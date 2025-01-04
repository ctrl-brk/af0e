export interface ActivationQsoModel {
  logId: number;
  date: Date;
  call: string;
  band: string | null;
  mode: string | null;
  satName: string | null;
  p2p: string[];
  long: number | null;
  lat: number | null;
}
