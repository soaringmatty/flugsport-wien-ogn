import { Component, OnDestroy, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, interval, takeUntil } from 'rxjs';
import { State } from 'src/app/store';
import { loadGliderList } from 'src/app/store/app/app.actions';
import { Flight } from 'src/ogn/models/flight.model';
import { GliderListItem } from 'src/ogn/models/glider-list-item.model';
import { GliderStatus } from 'src/ogn/models/glider-status';

@Component({
  selector: 'app-glider-list',
  templateUrl: './glider-list.component.html',
  styleUrls: ['./glider-list.component.scss']
})
export default class GliderListComponent implements OnInit, OnDestroy {
  gliderList: GliderListItem[] = [];
  displayedColumns: string[] = ['displayName', 'model', 'status', 'flightDuration', 'distanceFromHome', 'altitude'];
  private readonly updateListTimeout = 5000;
  private readonly onDestroy$ = new Subject<void>();

  constructor(private store: Store<State>) {
  }

  ngOnInit(): void {
    this.store
      .select((x) => x.app.gliderList)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((gliderList) => {
        this.gliderList = gliderList;
      });
    this.store.dispatch(loadGliderList());
    this.setupTimerForGliderPositionUpdates();
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  getDistanceFromHome(distance: number): string { 
    if (distance < 0) {
      return '';
    }
    if (distance < 1000) {
      return `${distance} m`;
    }
    return `${Math.round(distance / 1000)} km`;
  }

  getAltitude(altitude: number, status: GliderStatus): string { 
    if (altitude < 0 || status != GliderStatus.Flying) {
      return '';
    }
    return `${altitude} m`;
  }

  getFlightDuration(takeOffTimestamp: number, status: GliderStatus): string { 
    if (status != GliderStatus.Flying) {
      return '';
    }
    if (takeOffTimestamp == -1) {
      return 'Unbekannt';
    }
    return this.formatFlightDuration(takeOffTimestamp);
  }

  private formatFlightDuration(startTimestamp: number): string {
    if (startTimestamp) {
        const totalSeconds = Math.floor((+new Date() - +new Date(startTimestamp)) / 1000);

        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds - (hours * 3600)) / 60);
        const seconds = totalSeconds - (hours * 3600) - (minutes * 60);

        const hoursStr = hours > 0 ? hours.toString() + ':' : '';
        const minutesStr = minutes.toString().padStart(2, '0');
        const secondsStr = seconds.toString().padStart(2, '0');

        return `${hoursStr}${minutesStr}:${secondsStr}`;
    }
    return '';
  }

  private setupTimerForGliderPositionUpdates() {
    interval(this.updateListTimeout)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(() => {
        this.store.dispatch(loadGliderList())
      });
  }
}
