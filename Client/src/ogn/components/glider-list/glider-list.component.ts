import { Component, OnDestroy, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Subject, takeUntil } from 'rxjs';
import { State } from 'src/app/store';
import { Flight } from 'src/ogn/models/flight.model';

@Component({
  selector: 'app-glider-list',
  templateUrl: './glider-list.component.html',
  styleUrls: ['./glider-list.component.scss']
})
export default class GliderListComponent implements OnInit, OnDestroy {
  flights: Flight[] = [];
  private readonly onDestroy$ = new Subject<void>();

  constructor(private store: Store<State>) {
  }

  ngOnInit(): void {
    this.store
      .select((x) => x.app.flights)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((flights) => {
        this.flights = flights;
      });

      //Select flight path from store
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  // iterate through flights and add boolean property onGround that is true if plane is 10m above ground
  mapFlightsToOnGround(flights: Flight[]): Flight[] {
    return flights.map((flight) => {
      const onGround = flight.altitude < 10;
      return { ...flight, onGround };
    });
  }
