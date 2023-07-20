import { ChangeDetectionStrategy, Component, OnDestroy, OnInit } from '@angular/core';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { cloneDeep } from 'lodash';
import { Subject, takeUntil } from 'rxjs';
import { State } from 'src/app/store';
import { AppActionTypes, loadFlights, saveSettings } from 'src/app/store/app/app.actions';
import { MapSettings } from 'src/ogn/models/map-settings.model';

@Component({
  selector: 'app-settings-dialog',
  templateUrl: './settings-dialog.component.html',
  styleUrls: ['./settings-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsDialogComponent {
}
