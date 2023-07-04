import { createAction, props } from '@ngrx/store';
import { DepartureListItem } from 'src/ogn/models/departure-list-item.model';
import { Flight } from 'src/ogn/models/flight.model';
import { GliderListItem } from 'src/ogn/models/glider-list-item.model';
import { HistoryEntry } from 'src/ogn/models/history-entry.model';
import { MapSettings } from 'src/ogn/models/map-settings.model';

export enum AppActionTypes {
    loadFlights = '[App] Load Flights',
    loadFlightsSuccess = '[App] Load Flights Success',
    loadFlightsFailure = '[App] Load Flights Failure',
    loadFlightPath = '[App] Load Flight Path',
    loadFlightPathSuccess = '[App] Load Flight Path Success',
    loadFlightPathFailure = '[App] Load Flight Path Failure',
    loadFlightHistory = '[App] Load Flight History',
    loadFlightHistorySuccess = '[App] Load Flight History Success',
    loadFlightHistoryFailure = '[App] Load Flight History Failure',
    saveSettings = '[App] Save Settings',
    loadSettings = '[App] Load Settings',
    loadSettingsSuccess = '[App] Load Settings Success',
    selectFlight = '[App] Select Flight',
    loadGliderList = '[App] Load Glider List',
    loadGliderListSuccess = '[App] Load Glider List Success',
    loadGliderListFailure = '[App] Load Glider List Failure',
    loadDepartureList = '[App] Load Departure List',
    loadDepartureListSuccess = '[App] Load Departure List Success',
    loadDepartureListFailure = '[App] Load Departure List Failure',
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

export const loadFlightHistory = createAction(
    AppActionTypes.loadFlightHistory,
    props<{flarmId: string}>()
);

export const loadFlightHistorySuccess = createAction(
    AppActionTypes.loadFlightHistorySuccess,
    props<{flightHistory: HistoryEntry[]}>()
);

export const loadFlightHistoryFailure = createAction(
    AppActionTypes.loadFlightHistoryFailure,
    props<{error: any}>()
);

export const saveSettings = createAction(
    AppActionTypes.saveSettings,
    props<{settings: MapSettings}>()
);

export const loadSettings = createAction(
    AppActionTypes.loadSettings
);

export const loadSettingsSuccess = createAction(
    AppActionTypes.loadSettingsSuccess,
    props<{settings: MapSettings}>()
);

export const selectFlight = createAction(
    AppActionTypes.selectFlight,
    props<{flight: Flight | null}>()
);

export const loadGliderList = createAction(
    AppActionTypes.loadGliderList
);

export const loadGliderListSuccess = createAction(
    AppActionTypes.loadGliderListSuccess,
    props<{gliderList: GliderListItem[]}>()
);

export const loadGliderListFailure = createAction(
    AppActionTypes.loadGliderListFailure,
    props<{error: any}>()
);

export const loadDepartureList = createAction(
    AppActionTypes.loadDepartureList
);

export const loadDepartureListSuccess = createAction(
    AppActionTypes.loadDepartureListSuccess,
    props<{departureList: DepartureListItem[]}>()
);

export const loadDepartureListFailure = createAction(
    AppActionTypes.loadDepartureListFailure,
    props<{error: any}>()
);
