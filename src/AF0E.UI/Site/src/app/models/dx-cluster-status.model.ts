export interface DxClusterServerStatusModel {
  name: string;
  host: string;
  port: number;
  enabled: boolean;
  connected: boolean;
  reconnectCount: number;
  lastConnectUtc: Date | null;
  lastDisconnectUtc: Date | null;
  lastLineUtc: Date | null;
  lastSpotUtc: Date | null;
  lastErrorUtc: Date | null;
  lastError: string | null;
}

export interface DxClusterStatusModel {
  configured: boolean;
  running: boolean;
  lastAccessUtc: Date | null;
  lastStartUtc: Date | null;
  lastStopUtc: Date | null;
  cachedSpotCount: number;
  inactivityTimeout: string;
  reconnectDelay: string;
  servers: DxClusterServerStatusModel[];
}
