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
export class SettingsDialogComponent implements OnInit, OnDestroy {
  settings!: MapSettings
  private readonly onDestroy$ = new Subject<void>();

  constructor(private store: Store<State>) {

  }

  ngOnInit(): void {
    this.store.select(x => x.app.settings).pipe(
      takeUntil(this.onDestroy$)
    ).subscribe(settings => {
      this.settings = cloneDeep(settings);
    })
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  save(): void {
    this.store.dispatch(saveSettings({settings: this.settings}));
  }
}
