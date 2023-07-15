import { Injectable } from '@angular/core';
import { HistoryEntry } from '../models/history-entry.model';

export interface FlightEvent {
  timestamp: number;
  event: 'Departure' | 'Landing';
}

@Injectable({
  providedIn: 'root'
})
export class FlightAnalysationService {
  analyzeFlightData(history: HistoryEntry[]): FlightEvent[] {
    const flightEvents: FlightEvent[] = [];
    let isAirborne = false;

    const altitudeTolerance = 25;
    const minimumSpeed = 5; // meters per second
    const minimumGroundTime = 1 * 60 * 1000; // 5 minutes
    const minimumAirTime = 1 * 60 * 1000; // 5 minutes

    for (let i = 1; i < history.length; i++) {
      const prevEntry = history[i - 1];
      const currentEntry = history[i];

      const distance = this.getDistance(prevEntry.latitude, prevEntry.longitude, currentEntry.latitude, currentEntry.longitude);
      const timeDiff = currentEntry.timestamp - prevEntry.timestamp;
      const speed = distance / (timeDiff / 1000); // speed in meters per second

      const isOnGround = Math.abs(currentEntry.altitude - currentEntry.groundHeight) <= altitudeTolerance;

      if (isOnGround && speed < minimumSpeed) {
        if (!isAirborne) continue;

        // check if it's been on the ground for at least minimumGroundTime
        const groundTime = currentEntry.timestamp - flightEvents[flightEvents.length - 1].timestamp;
        if (groundTime >= minimumGroundTime) {
          flightEvents.push({ timestamp: currentEntry.timestamp, event: 'Landing' });
          isAirborne = false;
        }
      } else {
        if (isAirborne) continue;

        // check if it's been airborne for at least minimumAirTime
        const airTime = currentEntry.timestamp - (flightEvents.length > 0 ? flightEvents[flightEvents.length - 1].timestamp : history[0].timestamp);
        if (airTime >= minimumAirTime) {
          flightEvents.push({ timestamp: currentEntry.timestamp, event: 'Departure' });
          isAirborne = true;
        }
      }
    }

    return flightEvents;
  }

  getDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
    // approximate radius of earth in meters
    const R = 6371e3;

    const φ1 = lat1 * Math.PI/180; // φ, λ in radians
    const φ2 = lat2 * Math.PI/180;
    const Δφ = (lat2-lat1) * Math.PI/180;
    const Δλ = (lon2-lon1) * Math.PI/180;

    const a = Math.sin(Δφ/2) * Math.sin(Δφ/2) +
              Math.cos(φ1) * Math.cos(φ2) *
              Math.sin(Δλ/2) * Math.sin(Δλ/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));

    const d = R * c; // in meters
    return d;
  }
}
