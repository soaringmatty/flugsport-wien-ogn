import { Injectable } from '@angular/core';
import { Observable, interval, map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ClockService {

  private clock: Observable<Date>;

  constructor() {
    this.clock = interval(100).pipe(
      map(tick => new Date())
    );
  }

  getClock(): Observable<Date> {
    return this.clock;
  }

}