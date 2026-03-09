export interface KeyerStatusModel {
  ok: boolean;
  portOpen: boolean;
  hostOpen: boolean;
  revision: number | null;
  lastActivityUtc: string | null;
  idleSeconds: number | null;
  busy: boolean | null;
  wait: boolean | null;
  xoff: boolean | null;
  speedPotRaw: number | null;
  speedPotWpm: number | null;
  hostWpm: number | null;
  effectiveWpm: number | null;
  minWpm: number;
  maxWpm: number;
  wpmRange: number;
}
