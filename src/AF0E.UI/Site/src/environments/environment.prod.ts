import {LogLevel} from "../app/shared/log-level.enum";
import {commonEnvironment} from "./environment.common";

export const environment = {
  ...commonEnvironment,
  production: true,
  apiUrl: '/api/v1',
  logLevel: LogLevel.Error,
};
