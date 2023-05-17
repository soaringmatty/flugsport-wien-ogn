import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Flight } from '../models/flight.model';
import { api } from 'src/environments/api';
import { HistoryEntry } from '../models/history-entry.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient) { }

  getFlights(): Observable<Flight[]> {
    return this.http.get<Flight[]>(api.getFlights);
  }

  getFlightPath(flarmId: string): Observable<string> {
    const url = api.getFlightPath.replace('{id}', flarmId)
    return this.http.get(url, {responseType: 'text'});
  }

  getFlightHistory(flarmId: string): Observable<HistoryEntry[]> {
    const url = api.getFlightHistory.replace('{id}', flarmId)
    return this.http.get<number[][]>(url).pipe(
      map(response => response.map(rawEntry => {
        const historyEntry: HistoryEntry = {
          timestamp: rawEntry[0],
          latitude: rawEntry[2],
          longitude: rawEntry[3],
          altitude: rawEntry[4],
          groundHeight: rawEntry[5]
        }
        return historyEntry;
      }))
    );
  }
}
