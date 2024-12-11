import {LogLevel} from "../app/shared/log-level.enum";

export const environment = {
  production: false,
  //apiUrl: 'http://localhost:5200/api/v1', //if proxy is not use
  apiUrl: '/api/v1', //with proxy
  logLevel: LogLevel.Debug
};
