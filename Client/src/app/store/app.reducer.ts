import { createReducer, on } from '@ngrx/store';
import { loadFlights, loadFlightsSuccess } from './app.actions';
import { Flight } from 'src/ogn/models/flight.model';

export interface AppState {
  flights: Flight[];
}

export const initialState: AppState = {
  flights: []
};

export const appReducer = createReducer(
  initialState,
  on(loadFlightsSuccess, (state, {flights}) => {
    console.log('reducer loadFlightsSuccess')
    return {...state, flights }
  }),
);