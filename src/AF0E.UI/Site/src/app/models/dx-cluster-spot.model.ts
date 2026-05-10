export interface DxClusterSpotModel {
  sourceName: string;
  spotterCallsign: string;
  dxCallsign: string;
  frequencyKhz: number;
  comment: string;
  rawLine: string;
  spotTimeUtc: Date;
  receivedAtUtc: Date;
}
