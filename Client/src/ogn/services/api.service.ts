import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Flight } from '../models/flight.model';
import { api } from 'src/environments/api';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly getFlightPathUrl = 'https://localhost:/v2/track/{flarmId}'

  constructor(private http: HttpClient) { }

  getFlights(): Observable<Flight[]> {
    return this.http.get<Flight[]>(api.getFlights);
  }

  getFlightPath(flarmId: string): Observable<string> {
    const url = api.getFlightPath.replace('{id}', flarmId)
    return this.http.get<string>(url);
  }
}
