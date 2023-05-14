import { createReducer, on } from '@ngrx/store';
import { loadFlightPathSuccess, loadFlightsSuccess, loadSettingsSuccess, saveSettings } from './app.actions';
import { Flight } from 'src/ogn/models/flight.model';
import { MapSettings } from 'src/ogn/models/map-settings.model';
import { GliderType } from 'src/ogn/models/glider-type';
import { clubGliders, privateGliders } from 'src/ogn/constants/known-gliders';
import { defaultSettings } from 'src/ogn/services/settings.service';

export interface AppState {
  flights: Flight[];
  flightPath: string;
  settings: MapSettings
}

export const initialState: AppState = {
  flights: [],
  flightPath: '',
  settings: defaultSettings
};

export const appReducer = createReducer(
  initialState,
  on(loadFlightsSuccess, (state, {flights}) => {
    let filteredFlights = flights;
    switch (state.settings.gliderFilter) {
      case GliderType.club:
        filteredFlights = flights.filter(flight => clubGliders.find(glider => flight.flarmId && flight.flarmId === glider.FlarmId));
        break;
      case GliderType.private:
        const clubGliderFlights = flights.filter(flight => clubGliders.find(glider => flight.flarmId && flight.flarmId === glider.FlarmId))
        const privateGliderFlights = flights.filter(flight => privateGliders.find(glider => flight.flarmId && flight.flarmId === glider.FlarmId))
        filteredFlights = clubGliderFlights.concat(privateGliderFlights);
        break;
      default:
        filteredFlights = flights;
        break;
    }
    if (state.settings.hideGlidersOnGround) {
      filteredFlights = filteredFlights.filter(x => x.speed > 10 && x.heightAGL > 10);
    }
    return {
      ...state,
      flights: filteredFlights
    }
  }),
  on(loadFlightPathSuccess, (state, {encodedFlightPath}) => {
    return {
      ...state, 
      flightPath: encodedFlightPath 
    }
  }),
  on(saveSettings, (state, {settings}) => {
    return {
      ...state, 
      settings: settings
    }
  }),
  on(loadSettingsSuccess, (state, {settings}) => {
    return {
      ...state, 
      settings: settings
    }
  }),
);

