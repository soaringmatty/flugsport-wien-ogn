import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Flight } from '../models/flight.model';
import { api } from 'src/environments/api';
import { HistoryEntry } from '../models/history-entry.model';
import { GliderListItem } from '../models/glider-list-item.model';
import { DepartureListItem } from '../models/departure-list-item.model';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  constructor(private http: HttpClient) { }

  getFlights(maxLat: number, minLat: number, maxLng: number, minLng: number, selectedFlarmId?: string, glidersOnly?: boolean, clubGlidersOnly?: boolean): Observable<Flight[]> {
    let params = new HttpParams();
    params = params.append('maxLat', maxLat.toString());
    params = params.append('minLat', minLat.toString());
    params = params.append('maxLng', maxLng.toString());
    params = params.append('minLng', minLng.toString());
    if (selectedFlarmId) {
      params = params.append('selectedFlarmId', selectedFlarmId);
    }
    if (glidersOnly) {
      params = params.append('glidersOnly', glidersOnly);
    }
    if (clubGlidersOnly) {
      params = params.append('clubGlidersOnly', clubGlidersOnly);
    }

    return this.http.get<Flight[]>(api.getFlights, { params: params });
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

  getGliderList(includePrivateGliders: boolean = true): Observable<GliderListItem[]> {
    const url = api.getGliderList.replace('{pg}', includePrivateGliders.toString())
    return this.http.get<GliderListItem[]>(url);
  }

  getDepartureList(includePrivateGliders: boolean = true): Observable<DepartureListItem[]> {
    const url = api.getDepartureList.replace('{pg}', includePrivateGliders.toString())
    return this.http.get<DepartureListItem[]>(url);
  }
}
