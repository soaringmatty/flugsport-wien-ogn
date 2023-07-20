import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { Subject, interval, takeUntil } from 'rxjs';
import { State } from 'src/app/store';
import { loadGliderList } from 'src/app/store/app/app.actions';
import { mobileLayoutBreakpoints } from 'src/ogn/constants/layouts';
import { Flight } from 'src/ogn/models/flight.model';
import { GliderListItem } from 'src/ogn/models/glider-list-item.model';
import { GliderStatus } from 'src/ogn/models/glider-status';
import { GliderType } from 'src/ogn/models/glider-type';
import { MapSettings } from 'src/ogn/models/map-settings.model';

@Component({
  selector: 'app-glider-list',
  templateUrl: './glider-list.component.html',
  styleUrls: ['./glider-list.component.scss']
})
export default class GliderListComponent implements OnInit, OnDestroy {
  gliderList: GliderListItem[] = [];
  displayedColumns: string[] = ['displayName', 'model', 'status', 'flightDuration', 'distanceFromHome', 'altitude', 'action'];
  isMobilePortrait: boolean = false;
  settings!: MapSettings
  private readonly updateListTimeout = 5000;
  private readonly onDestroy$ = new Subject<void>();

  constructor(private breakpointObserver: BreakpointObserver, private store: Store<State>, private router: Router) { }

  ngOnInit(): void {
    this.breakpointObserver.observe(mobileLayoutBreakpoints).subscribe(result => {
      this.isMobilePortrait = result.matches;
    });
    this.store
      .select((x) => x.app.gliderList)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((gliderList) => {
        this.gliderList = gliderList;
      });
    this.store
      .select((x) => x.app.settings)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(settings => {
        this.settings = settings
      });
    this.store.dispatch(loadGliderList({includePrivateGliders: this.settings?.gliderFilterInLists === GliderType.private}));
    this.setupTimerForGliderPositionUpdates();
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  navigateToMap(lat: number, lon: number): void {
    //this.router.navigate(['/map', lat, lon]);
    this.router.navigate(['/map', { lat, lon }]);
    //this.router.navigate(['/map']);
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
        this.store.dispatch(loadGliderList({includePrivateGliders: this.settings?.gliderFilterInLists === GliderType.private}))
      });
  }
}
