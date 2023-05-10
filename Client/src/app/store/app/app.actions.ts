import { createAction, props } from '@ngrx/store';
import { Flight } from 'src/ogn/models/flight.model';

export enum AppActionTypes {
    loadFlights = '[App] Load Flights',
    loadFlightsSuccess = '[App] Load Flights Success',
    loadFlightsFailure = '[App] Load Flights Failure',
    loadFlightPath = '[App] Load Flight Path',
    loadFlightPathSuccess = '[App] Load Flight Path Success',
    loadFlightPathFailure = '[App] Load Flight Path Failure'
  }

export const loadFlights = createAction(
    AppActionTypes.loadFlights
);

export const loadFlightsSuccess = createAction(
    AppActionTypes.loadFlightsSuccess,
    props<{flights: Flight[]}>()
);

export const loadFlightsFailure = createAction(
    AppActionTypes.loadFlightsFailure,
    props<{error: any}>()
);

export const loadFlightPath = createAction(
    AppActionTypes.loadFlightPath,
    props<{flarmId: string}>()
);

export const loadFlightPathSuccess = createAction(
    AppActionTypes.loadFlightPathSuccess,
    props<{encodedFlightPath: string}>()
);

export const loadFlightPathFailure = createAction(
    AppActionTypes.loadFlightPathFailure,
    props<{error: any}>()
);

// export const AppActions = createActionGroup({
//   source: 'App',
//   events: {
//     'LoadFlights Apps': emptyProps(),
//     'LoadFlights Apps Success': props<{ data: unknown }>(),
//     'LoadFlights Apps Failure': props<{ error: unknown }>(),
//   }
// });
