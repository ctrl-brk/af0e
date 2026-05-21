export interface DxClusterFrequencyWindowModel {
  minFrequencyKhz: number | null;
  maxFrequencyKhz: number | null;
}

export interface DxClusterFilterModel {
  name: string;
  callsignPatterns?: string | null;
  frequencyWindows?: DxClusterFrequencyWindowModel[] | null;
  modes?: string[] | null;
  invalidCallsignPatterns: string[];
}
