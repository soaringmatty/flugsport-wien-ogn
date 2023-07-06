import { Component, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { SettingsDialogComponent } from 'src/ogn/components/settings-dialog/settings-dialog.component';
import { State } from './store';
import { loadSettings } from './store/app/app.actions';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationService } from 'src/ogn/services/notification.service';
import { th } from 'date-fns/locale';
import { Notification } from 'src/ogn/models/notification.model';
import { NotificationType } from 'src/ogn/models/notification-type';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  dialogRef!: MatDialogRef<SettingsDialogComponent, any>;
  isMobilePortrait: boolean = false;

  constructor(
    public settingsDialog: MatDialog,
    private store: Store<State>,
    private snackBar: MatSnackBar,
    private notificationService: NotificationService,
    private breakpointObserver: BreakpointObserver, ) {
      this.notificationService.notificationRequested.subscribe(notification => this.openSnackBar(notification));
  }

  ngOnInit(): void {
    this.store.dispatch(loadSettings())
    this.breakpointObserver.observe([
      Breakpoints.HandsetPortrait
    ]).subscribe(result => {
      this.isMobilePortrait = result.matches;
    });
  }

  openSnackBar(notification: Notification) {
    let panelClass;
    console.log(notification);
    switch(notification.type) {
      case NotificationType.Info:
        panelClass = 'snack-bar-info';
        break;
      case NotificationType.Success:
        panelClass = 'snack-bar-success';
        break;
      case NotificationType.Warning:
        panelClass = 'snack-bar-warn';
        break;
      case NotificationType.Error:
        panelClass = ['snack-bar-error'];
        break;
      default:
        panelClass = '';
    }
    this.snackBar.open(notification.message, undefined, {
      duration: 3500,
      panelClass: panelClass,
    });
  }

  openDialog(): void {
    this.dialogRef = this.settingsDialog.open(SettingsDialogComponent);
  }

  closeDialog(): void {
    this.dialogRef?.close();
  }
}
