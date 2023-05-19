import { EventEmitter, Injectable } from '@angular/core';
import { Notification } from '../models/notification.model';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  notificationRequested = new EventEmitter<Notification>();

  constructor() { }

  notify(notification: Notification) {
    this.notificationRequested.emit(notification);
  }
}
