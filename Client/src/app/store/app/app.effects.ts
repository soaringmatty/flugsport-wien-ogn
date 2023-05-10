import { Injectable } from "@angular/core";
import { Actions, createEffect, ofType } from "@ngrx/effects";
import * as AppActions from './app.actions'
import { catchError, exhaustMap, map, of } from "rxjs";
import { ApiService } from "src/ogn/services/api.service";

@Injectable()
export class AppEffects {
    constructor(
      private actions$: Actions,
      private apiService: ApiService
    ) {}
 
  loadFlights$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.loadFlights),
    exhaustMap(() => this.apiService.getFlights()
        .pipe(
            map(flights => {
                return AppActions.loadFlightsSuccess({flights})
            }),
            catchError(error => {
                console.log(error);
                return of(AppActions.loadFlightsFailure({error}))
            })
      ))
    )
  );

  loadFlightPath$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.loadFlightPath),
    exhaustMap((action) => this.apiService.getFlightPath(action.flarmId)
        .pipe(
            map(encodedFlightPath => {
                return AppActions.loadFlightPathSuccess({encodedFlightPath})
            }),
            catchError(error => {
                console.log(error);
                return of(AppActions.loadFlightPathFailure({error}))
            })
      ))
    )
  );
}