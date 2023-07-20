import { EventEmitter, Injectable } from '@angular/core';

export interface MarkerLocationUpdate {
  longitude: number;
  latitude: number;
  rotation: number;
}

@Injectable({
  providedIn: 'root'
})
export class MapBarogramSyncService {
  markerLocationUpdateRequested: EventEmitter<MarkerLocationUpdate>;

  constructor() {
    this.markerLocationUpdateRequested = new EventEmitter();
  }

  updateLocationMarkerOnMap(markerLocationUpdate?: MarkerLocationUpdate): void {
    this.markerLocationUpdateRequested.emit(markerLocationUpdate)
  }
}
