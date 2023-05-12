import { Component, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { SettingsDialogComponent } from 'src/ogn/components/settings-dialog/settings-dialog.component';
import { State } from './store';
import { loadSettings } from './store/app/app.actions';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  dialogRef!: MatDialogRef<SettingsDialogComponent, any>;

  constructor(public settingsDialog: MatDialog, private store: Store<State>) {
  }

  ngOnInit(): void {
    this.store.dispatch(loadSettings())
  }

  openDialog(): void {
    this.dialogRef = this.settingsDialog.open(SettingsDialogComponent);
  }

  closeDialog(): void {
    this.dialogRef?.close();
  }
}
