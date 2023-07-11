import { Component } from '@angular/core';
import { Store } from '@ngrx/store';
import { cloneDeep } from 'lodash';
import { State } from 'src/app/store';
import { Subject, takeUntil } from 'rxjs';
import { saveSettings } from 'src/app/store/app/app.actions';
import { MapSettings } from 'src/ogn/models/map-settings.model';
import config from '../../../../package.json'

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent {
  settings!: MapSettings;
  versionNumber = config.version;
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
