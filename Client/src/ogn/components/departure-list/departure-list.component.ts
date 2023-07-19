import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { interval, Subject, takeUntil, timestamp } from 'rxjs';
import { State } from 'src/app/store';
import { loadDepartureList } from 'src/app/store/app/app.actions';
import { mobileLayoutBreakpoints } from 'src/ogn/constants/layouts';
import { DepartureListItem } from 'src/ogn/models/departure-list-item.model';
import { GliderType } from 'src/ogn/models/glider-type';
import { MapSettings } from 'src/ogn/models/map-settings.model';

@Component({
  selector: 'app-departure-list',
  templateUrl: './departure-list.component.html',
  styleUrls: ['./departure-list.component.scss']
})
export class DepartureListComponent implements OnInit, OnDestroy {
  departureList: DepartureListItem[] = [];
  isMobilePortrait: boolean = false;
  settings!: MapSettings
  private readonly updateListTimeout = 10000;
  private readonly onDestroy$ = new Subject<void>();

  constructor(private breakpointObserver: BreakpointObserver, private store: Store<State>, private router: Router) { }

  ngOnInit(): void {
    this.breakpointObserver.observe(mobileLayoutBreakpoints).subscribe(result => {
      this.isMobilePortrait = result.matches;
    });
    this.store
      .select((x) => x.app.departureList)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((departureList) => {
        this.departureList = departureList;
      });
      this.store
      .select((x) => x.app.settings)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(settings => {
        this.settings = settings
      });
    this.store.dispatch(loadDepartureList({includePrivateGliders: this.settings?.gliderFilterInLists === GliderType.private}));
    this.setupTimerForGliderPositionUpdates();
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  getFlightDuration(takeOffTimestamp: number | undefined, landingTimestamp: number | undefined): string {
    return this.formatFlightDuration(takeOffTimestamp, landingTimestamp);
  }

  getReadableTimetamp(timestamp: number | undefined): string {
    if (!timestamp) {
      return ''
    }
    var date = new Date(timestamp * 1000);
    var hours = date.getHours();
    var minutes = date.getMinutes();

    // Add leading zero if necessary
    const hoursString = hours < 10 ? '0' + hours : hours;
    const minutesString = minutes < 10 ? '0' + minutes : minutes;

    return hoursString + ':' + minutesString;
  }

  private formatFlightDuration(departureTimestamp: number | undefined, landingTimestamp: number | undefined): string {
    if (departureTimestamp) {
        let totalSeconds = Math.floor((+new Date() - +new Date(departureTimestamp * 1000)) / 1000);
        if (landingTimestamp) {
          totalSeconds = Math.floor((+new Date(landingTimestamp * 1000) - +new Date(departureTimestamp * 1000)) / 1000);
        }
        const hours = Math.floor(totalSeconds / 3600);
        const minutes = Math.floor((totalSeconds - (hours * 3600)) / 60);

        const hoursStr = hours.toString().padStart(2, '0')
        const minutesStr = minutes.toString().padStart(2, '0');

        return `${hoursStr}:${minutesStr}`;
    }
    return '';
  }

  private setupTimerForGliderPositionUpdates() {
    interval(this.updateListTimeout)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(() => {
        this.store.dispatch(loadDepartureList({includePrivateGliders: this.settings?.gliderFilterInLists === GliderType.private}))
      });
  }
}
