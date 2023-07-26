import { Injectable } from '@angular/core';
import { HistoryEntry } from '../models/history-entry.model';

export interface FlightEvent {
  timestamp: number;
  event: FlightEventType;
}

export enum FlightEventType {
  departure = 0,
  landing = 1
}

@Injectable({
  providedIn: 'root'
})
export class FlightAnalysationService {
  getHistorySinceLastTakeoff(history: HistoryEntry[]) {
    const analyzedFlight = this.analyzeFlightData(history);
    const lastTakeoffTimestamp = this.getLastTakeoffTimestamp(analyzedFlight);
    if (lastTakeoffTimestamp) {
      const filteredHistory = history.filter(entry => entry.timestamp >= lastTakeoffTimestamp);
      return filteredHistory;
    }
    return history;
  }

  analyzeFlightData(history: HistoryEntry[]): FlightEvent[] {
    const flightEvents: FlightEvent[] = [];
    let isAirborne = false;
    let lastGroundTimestamp = history[0].timestamp;  // Initialize with the first timestamp

    const altitudeTolerance = 25;
    const minimumSpeed = 5; // meters per second
    const minimumGroundTime = 0.5 * 60 * 1000; // 30 seconds
    const minimumAirTime = 1 * 60 * 1000; // 1 minute

    for (let i = 1; i < history.length; i++) {
      const prevEntry = history[i - 1];
      const currentEntry = history[i];

      const distance = this.getDistance(prevEntry.latitude, prevEntry.longitude, currentEntry.latitude, currentEntry.longitude);
      const timeDiff = currentEntry.timestamp - prevEntry.timestamp;
      const speed = distance / (timeDiff / 1000); // speed in meters per second

      const isOnGround = Math.abs(currentEntry.altitude - currentEntry.groundHeight) <= altitudeTolerance;

      if (isOnGround && speed < minimumSpeed) {
        lastGroundTimestamp = currentEntry.timestamp;  // Update last ground timestamp
        if (!isAirborne) continue;

        // check if it's been on the ground for at least minimumGroundTime
        const groundTime = currentEntry.timestamp - flightEvents[flightEvents.length - 1].timestamp;
        if (groundTime >= minimumGroundTime) {
          flightEvents.push({ timestamp: currentEntry.timestamp, event: FlightEventType.landing });
          isAirborne = false;
        }
      } else {
        if (isAirborne) continue;

        // check if it's been airborne for at least minimumAirTime
        const airTime = currentEntry.timestamp - (flightEvents.length > 0 ? flightEvents[flightEvents.length - 1].timestamp : history[0].timestamp);
        if (airTime >= minimumAirTime) {
          // Use the lastGroundTimestamp for the 'Departure' event
          flightEvents.push({ timestamp: lastGroundTimestamp, event: FlightEventType.departure });
          isAirborne = true;
        }
      }
    }

    return flightEvents;
  }

  getLastTakeoffTimestamp(flightEvents: FlightEvent[]): number | null {
    // Find the last 'Departure' event
    for (let i = flightEvents.length - 1; i >= 0; i--) {
      if (flightEvents[i].event === FlightEventType.departure) {
        return flightEvents[i].timestamp;
      }
    }

    // If no 'Departure' event is found, return null
    return null;
  }

  private getDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
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
