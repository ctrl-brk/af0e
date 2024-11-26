export enum NotificationMessageSeverity {
    Success = 1,
    Info = 2,
    Warn = 3,
    Error = 4
}

export interface NotificationMessage {
    severity: NotificationMessageSeverity;
    summary?: string;
    message?: string;
    sticky?: boolean;
    timeout?: number;
}

export class NotificationMessageModel implements NotificationMessage {
    constructor(
        public severity: NotificationMessageSeverity,
        public summary?: string,
        public message?: string,
        public sticky?: boolean,
        public timeout?: number
    ) {}
}
