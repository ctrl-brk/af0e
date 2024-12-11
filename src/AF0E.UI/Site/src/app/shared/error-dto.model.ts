import {ErrorSource} from "./error-source.enum";
import {ErrorSeverity} from "./error-severity.enum";

export class ErrorDtoModel {
    constructor(
        public source: ErrorSource,
        public severity: ErrorSeverity,
        public message: string,
        public fullMessage: string,
        public data: any
    ) {}
}
