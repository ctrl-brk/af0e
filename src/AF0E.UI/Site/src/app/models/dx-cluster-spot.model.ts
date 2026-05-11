export interface DxClusterSpotModel {
  sourceName: string;
  spotterCallsign: string;
  dxCallsign: string;
  frequencyKhz: number;
  mode: string | null;
  comment: string;
  rawLine: string;
  spotTimeUtc: Date;
  receivedAtUtc: Date;
}
