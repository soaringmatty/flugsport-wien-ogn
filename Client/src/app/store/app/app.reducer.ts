import { createReducer, on } from '@ngrx/store';
import { loadDepartureListSuccess, loadFlightHistorySuccess, loadFlightPathSuccess, loadFlightsSuccess, loadGliderListSuccess, loadSettingsSuccess, saveSettings, selectFlight } from './app.actions';
import { Flight } from 'src/ogn/models/flight.model';
import { MapSettings } from 'src/ogn/models/map-settings.model';
import { GliderType } from 'src/ogn/models/glider-type';
import { clubGliders, privateGliders } from 'src/ogn/constants/known-gliders';
import { defaultSettings } from 'src/ogn/services/settings.service';
import { HistoryEntry } from 'src/ogn/models/history-entry.model';
import { isEqual } from 'lodash';
import { GliderListItem } from 'src/ogn/models/glider-list-item.model';
import { gl } from 'date-fns/locale';
import { DepartureListItem } from 'src/ogn/models/departure-list-item.model';

export interface AppState {
  flights: Flight[];
  selectedFlight: Flight | null;
  flightPath: string;
  flightHistory: HistoryEntry[];
  settings: MapSettings
  gliderList: GliderListItem[];
  departureList: DepartureListItem[];
}

export const initialState: AppState = {
  flights: [],
  selectedFlight: null,
  flightPath: '',
  flightHistory: [],
  settings: defaultSettings,
  gliderList: [],
  departureList: [],
};

export const appReducer = createReducer(
  initialState,
  on(loadFlightsSuccess, (state, {flights}) => {
    let filteredFlights = flights;
    // Filter list of flights depending on settings (club / private / all)
    switch (state.settings.gliderFilterOnMap) {
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
    // Filter out gliders that are on ground if selected in settings
    if (state.settings.hideGlidersOnGround) {
      filteredFlights = filteredFlights.filter(x => x.speed > 10 && x.heightAGL > 10);
    }
    // Update selectedFlight in store if any property has been changed
    let updatedSelectedFlight: Flight | undefined
    if (state.selectedFlight) {
      updatedSelectedFlight = filteredFlights.find(x => x.flarmId === state.selectedFlight?.flarmId);
      const hasChanges = !isEqual(updatedSelectedFlight?.timestamp, state.selectedFlight?.timestamp);
      if (updatedSelectedFlight && hasChanges) {
        // If flight history is already loaded, add new history entry
        if (state.flightHistory?.length > 0) {
          const newHistoryEntry: HistoryEntry = {
            timestamp: updatedSelectedFlight.timestamp,
            altitude: updatedSelectedFlight.heightMSL,
            latitude: updatedSelectedFlight.latitude,
            longitude: updatedSelectedFlight.longitude,
            groundHeight: updatedSelectedFlight.heightMSL - updatedSelectedFlight.heightAGL
          }
          return {
            ...state,
            flights: filteredFlights,
            selectedFlight: updatedSelectedFlight,
            flightHistory: [...state.flightHistory, newHistoryEntry]
          }
        }
        return {
          ...state,
          flights: filteredFlights,
          selectedFlight: updatedSelectedFlight
        }
      }
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
  on(loadFlightHistorySuccess, (state, {flightHistory}) => {
    return {
      ...state,
      flightHistory: flightHistory
    }
  }),
  on(selectFlight, (state, {flight}) => {
    return {
      ...state,
      selectedFlight: flight
    }
  }),
  on(loadGliderListSuccess, (state, {gliderList}) => {
    return {
      ...state,
      gliderList: gliderList
    }
  }),
  on(loadDepartureListSuccess, (state, {departureList}) => {
    return {
      ...state,
      departureList: departureList
    }
  }),
);

