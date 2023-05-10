import { createReducer, on } from '@ngrx/store';
import { loadFlightPathSuccess, loadFlights, loadFlightsSuccess } from './app.actions';
import { Flight } from 'src/ogn/models/flight.model';

export interface AppState {
  flights: Flight[];
  flightPath: string;
}

export const initialState: AppState = {
  flights: [],
  flightPath: ''
};

export const appReducer = createReducer(
  initialState,
  on(loadFlightsSuccess, (state, {flights}) => {
    return {
      ...state, 
      flights 
    }
  }),
  on(loadFlightPathSuccess, (state, {encodedFlightPath}) => {
    return {
      ...state, 
      flightPath: encodedFlightPath 
    }
  }),
);

