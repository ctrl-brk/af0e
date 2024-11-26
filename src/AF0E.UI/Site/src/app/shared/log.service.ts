import {environment} from "../../environments/environment";
import {LogLevel} from "./log-level.enum";

export class LogService {
  public debug(...obj: any[]) {
    if ( environment.logLevel > LogLevel.Debug ) return;
    console.log(obj);
  }

  //noinspection JSMethodCanBeStatic
  public warn(...obj: any[]) {
    if ( environment.logLevel > LogLevel.Warning ) return;
    console.warn(obj);
  }

  //noinspection JSMethodCanBeStatic
  public error(...obj: any[]) {
    console.error(obj);
  }

  public alert(msg: any) {
    alert(msg);
  }

  public jsonError(msg?: any) {
    this.alert(msg ? msg : "Json deserialization error");
  }
}
