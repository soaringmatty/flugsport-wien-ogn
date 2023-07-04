import { Injectable } from "@angular/core";
import { Actions, createEffect, ofType } from "@ngrx/effects";
import * as AppActions from './app.actions'
import { catchError, exhaustMap, map, of, switchMap, tap } from "rxjs";
import { ApiService } from "src/ogn/services/api.service";
import { SettingsService } from "src/ogn/services/settings.service";
import { NotificationService } from "src/ogn/services/notification.service";
import { messages } from "src/ogn/constants/messages";
import { NotificationType } from "src/ogn/models/notification-type";

@Injectable()
export class AppEffects {
    constructor(
      private actions$: Actions,
      private apiService: ApiService,
      private settingsService: SettingsService,
      private notificationService: NotificationService
    ) {}

  loadFlights$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.loadFlights),
    exhaustMap(() => this.apiService.getFlights()
        .pipe(
            map(flights => {
                return AppActions.loadFlightsSuccess({flights})
            }),
            catchError(error => {
                this.notificationService.notify({
                  message: messages.defaultNetworkError,
                  type: NotificationType.Error
                });
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

  loadFlightHistory$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.loadFlightHistory),
    exhaustMap((action) => this.apiService.getFlightHistory(action.flarmId)
        .pipe(
            map(flightHistory => {
                return AppActions.loadFlightHistorySuccess({flightHistory})
            }),
            catchError(error => {
                console.log(error);
                return of(AppActions.loadFlightHistoryFailure({error}))
            })
      ))
    )
  );

  saveSettings$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.saveSettings),
    tap(action => {
      this.settingsService.saveSettings(action.settings);
      console.log('Saved settings', action.settings)
    })
  ),
  { dispatch: false }
  );

  loadSettings$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.loadSettings),
    switchMap(() => {
      const settings = this.settingsService.loadSettings();
      console.log('Loaded settings', settings)
      return of(AppActions.loadSettingsSuccess({ settings }));
    })
  ));

  loadGliderList$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.loadGliderList),
    exhaustMap(() => this.apiService.getGliderList()
      .pipe(
        map(gliderList => {
            return AppActions.loadGliderListSuccess({gliderList})
        }),
        catchError(error => {
          this.notificationService.notify({
            message: messages.defaultNetworkError,
            type: NotificationType.Error
          });
            return of(AppActions.loadGliderListFailure({error}))
        })
    ))
    )
  );

  loadDepartureList$ = createEffect(() => this.actions$.pipe(
    ofType(AppActions.loadDepartureList),
    exhaustMap(() => this.apiService.getDepartureList()
      .pipe(
        map(departureList => {
            return AppActions.loadDepartureListSuccess({departureList})
        }),
        catchError(error => {
          this.notificationService.notify({
            message: messages.defaultNetworkError,
            type: NotificationType.Error
          });
            return of(AppActions.loadDepartureListFailure({error}))
        })
    ))
    )
  );
}
