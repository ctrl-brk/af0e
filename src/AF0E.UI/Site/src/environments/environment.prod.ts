import {LogLevel} from "../app/shared/log-level.enum";
import {environmentSecrets} from "./environment-secrets";

export const environment = {
  production: true,
  apiUrl: '/api/v1',
  mapBoxKey: environmentSecrets.mapBoxKey,
  logLevel: LogLevel.Error
};
