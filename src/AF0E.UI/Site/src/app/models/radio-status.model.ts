export interface RadioStatus {
  ok: boolean;
  frequencyHz: number;
  mode: string;
  dataModeOn: boolean;
  noiseReductionOn: boolean;
  filter: number | null
}
