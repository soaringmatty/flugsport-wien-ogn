import { Injectable } from "@angular/core";
import { Actions, createEffect, ofType } from "@ngrx/effects";
import { AppActionTypes, loadFlights, loadFlightsFailure, loadFlightsSuccess } from "./app.actions";
import { EMPTY, catchError, exhaustMap, map, of } from "rxjs";
import { ApiService } from "src/ogn/services/api.service";

@Injectable()
export class AppEffects {
    constructor(
      private actions$: Actions,
      private apiService: ApiService
    ) {}
 
  loadFlights$ = createEffect(() => this.actions$.pipe(
    ofType(AppActionTypes.loadFlights),
    exhaustMap(() => this.apiService.getFlights()
        .pipe(
            map(flights => {
                console.log('loadFlights Effects', flights);
                return loadFlightsSuccess({flights})
            }),
            catchError(error => {
                console.log(error);
                return of(loadFlightsFailure({error}))
            })
      ))
    )
  );
}