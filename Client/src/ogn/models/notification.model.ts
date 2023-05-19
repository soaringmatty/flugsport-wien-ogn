import { NotificationType } from "./notification-type";

export interface Notification {
    message: string;
    type: NotificationType;
}