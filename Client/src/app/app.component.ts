import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { SettingsDialogComponent } from 'src/ogn/components/settings-dialog/settings-dialog.component';
import { State } from './store';
import { AppActionTypes, loadSettings } from './store/app/app.actions';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationService } from 'src/ogn/services/notification.service';
import { th } from 'date-fns/locale';
import { Notification } from 'src/ogn/models/notification.model';
import { NotificationType } from 'src/ogn/models/notification-type';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { ChangelogComponent } from './components/changelog/changelog.component';
import { SettingsService } from 'src/ogn/services/settings.service';
import { Subject, takeUntil } from 'rxjs';
import { Actions, ofType } from '@ngrx/effects';
import { MapSettings } from 'src/ogn/models/map-settings.model';
import { mobileLayoutBreakpoints } from 'src/ogn/constants/layouts';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  settingsDialogRef!: MatDialogRef<SettingsDialogComponent, any>;
  changelogDialogRef!: MatDialogRef<ChangelogComponent, any>;
  isMobilePortrait: boolean = false;
  settings!: MapSettings;
  private readonly onDestroy$ = new Subject<void>();

  constructor(
    public settingsDialog: MatDialog,
    public changelogDialog: MatDialog,
    private store: Store<State>,
    private actions: Actions,
    private snackBar: MatSnackBar,
    private notificationService: NotificationService,
    private breakpointObserver: BreakpointObserver,
    private settingsService: SettingsService) {
      this.notificationService.notificationRequested.subscribe(notification => this.openSnackBar(notification));
  }

  ngOnInit(): void {
    // Show changelog version if new settings
    this.actions
      .pipe(
        takeUntil(this.onDestroy$),
        ofType(AppActionTypes.loadSettingsSuccess)
      )
      .subscribe((action: any) => {
        const settings = action.settings as MapSettings
        if (this.settingsService.isNewVersion && settings.showChangelogForNewVersion) {
          this.openChangelogDialog();
        }
      })
    this.store.dispatch(loadSettings())
    this.breakpointObserver.observe(mobileLayoutBreakpoints).subscribe(result => {
      this.isMobilePortrait = result.matches;
    });
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  openSnackBar(notification: Notification) {
    let panelClass: string[] = [];
    console.log(notification);
    switch(notification.type) {
      case NotificationType.Info:
        panelClass = ['snack-bar-info'];
        break;
      case NotificationType.Success:
        panelClass = ['snack-bar-success'];
        break;
      case NotificationType.Warning:
        panelClass = ['snack-bar-warn'];
        break;
      case NotificationType.Error:
        panelClass = ['snack-bar-error'];
        break;
      default:
        break;
    }
    if (this.isMobilePortrait) {
      panelClass.push('snackbar-mobile')
    }
    this.snackBar.open(notification.message, undefined, {
      duration: 2000,
      panelClass: panelClass,
    });
  }

  openSettingsDialog(): void {
    this.settingsDialogRef = this.settingsDialog.open(SettingsDialogComponent);
  }

  closeSettingsDialog(): void {
    this.settingsDialogRef?.close();
  }

  openChangelogDialog(): void {
    this.changelogDialogRef = this.changelogDialog.open(ChangelogComponent);
  }

  closeChangelogDialog(): void {
    this.changelogDialogRef?.close();
  }
}
