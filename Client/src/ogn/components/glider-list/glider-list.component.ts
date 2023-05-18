import { Component, OnDestroy, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, interval, takeUntil } from 'rxjs';
import { State } from 'src/app/store';
import { loadGliderList } from 'src/app/store/app/app.actions';
import { Flight } from 'src/ogn/models/flight.model';
import { GliderListItem } from 'src/ogn/models/glider-list-item.model';

@Component({
  selector: 'app-glider-list',
  templateUrl: './glider-list.component.html',
  styleUrls: ['./glider-list.component.scss']
})
export default class GliderListComponent implements OnInit, OnDestroy {
  gliderList: GliderListItem[] = [];
  displayedColumns: string[] = ['displayName', 'model', 'status', 'flightTime', 'distanceFromHome', 'altitude'];
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

  private setupTimerForGliderPositionUpdates() {
    interval(this.updateListTimeout)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(() => {
        this.store.dispatch(loadGliderList())
      });
  }
}
