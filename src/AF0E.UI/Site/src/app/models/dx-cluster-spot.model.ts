export interface DxClusterSpotModel {
  sourceName: string;
  spotterCallsign: string;
  dxCallsign: string;
  dxccEntityCode?: number | null;
  dxccEntityName: string | null;
  dxccCountryCode: string | null;
  dxccWorkedStatus: string | null;
  frequencyKhz: number;
  mode: string | null;
  comment: string;
  rawLine: string;
  spotTimeUtc: Date;
  receivedAtUtc: Date;
}
