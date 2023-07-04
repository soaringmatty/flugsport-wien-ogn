import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { interval, Subject, takeUntil, timestamp } from 'rxjs';
import { State } from 'src/app/store';
import { loadDepartureList } from 'src/app/store/app/app.actions';
import { DepartureListItem } from 'src/ogn/models/departure-list-item.model';

@Component({
  selector: 'app-departure-list',
  templateUrl: './departure-list.component.html',
  styleUrls: ['./departure-list.component.scss']
})
export class DepartureListComponent implements OnInit, OnDestroy {
  departureList: DepartureListItem[] = [];
  isMobilePortrait: boolean = false;
  private readonly updateListTimeout = 10000;
  private readonly onDestroy$ = new Subject<void>();

  constructor(private breakpointObserver: BreakpointObserver, private store: Store<State>, private router: Router) { }

  ngOnInit(): void {
    this.breakpointObserver.observe([
      Breakpoints.HandsetPortrait
    ]).subscribe(result => {
      this.isMobilePortrait = result.matches;
    });
    this.store
      .select((x) => x.app.departureList)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((departureList) => {
        this.departureList = departureList;
      });
    this.store.dispatch(loadDepartureList());
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
    var hours = date.getUTCHours();
    var minutes = date.getUTCMinutes();

    // Add leading zero if necessary
    const hoursString = hours < 10 ? '0' + hours : hours;
    const minutesString = minutes < 10 ? '0' + minutes : minutes;

    return hoursString + ':' + minutesString;
  }

  private formatFlightDuration(departureTimestamp: number | undefined, landingTimestamp: number | undefined): string {
    console.log('formatFlightDuration', departureTimestamp, landingTimestamp);
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
        this.store.dispatch(loadDepartureList())
      });
  }
}
