import { isDevMode } from '@angular/core';
import {
  ActionReducerMap,
  MetaReducer
} from '@ngrx/store';
import { appReducer, AppState } from './app/app.reducer';

export interface State {
  app: AppState
}

export const reducers: ActionReducerMap<State> = {
  app: appReducer
};

export const metaReducers: MetaReducer<State>[] = isDevMode() ? [] : [];
