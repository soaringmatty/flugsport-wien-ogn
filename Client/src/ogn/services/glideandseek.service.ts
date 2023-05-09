import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { Flight } from '../models/flight.model';

@Injectable({
  providedIn: 'root'
})
export class GlideAndSeekService {
  private readonly getFlightsUrl = 'https://api.glideandseek.com/v2/aircraft?showAllTracks=false&a=48.72&b=46.65&c=16.70&d=11.00';
  private readonly getFlightPathUrl = 'https://api.glideandseek.com/v2/track/{flarmId}'

  constructor(private http: HttpClient) { }

  getFlights(): Observable<Flight[]> {
    const httpOptions = {
      //headers: new HttpHeaders({ 'Access-Control-Allow-Origin' : '*' })
    }
    return this.http.get<{ success: boolean, message: any[] }>(this.getFlightsUrl, httpOptions)
      .pipe(
        map(response => response.message.map(rawFlight => {
          return {
            FlarmId: rawFlight.flarmID,
            DisplayName: rawFlight.displayName,
            Registration: rawFlight.registration,
            Type: rawFlight.type,
            Model: rawFlight.model,
            Latitude: rawFlight.lat,
            Longitude: rawFlight.lng,
            HeightMSL: rawFlight.altitude,
            HeightAGL: rawFlight.agl,
            Timestamp: rawFlight.timestamp,
            Speed: rawFlight.speed,
            Vario: rawFlight.vario,
            VarioAverage: rawFlight.varioAverage,
            Receiver: rawFlight.receiver,
            ReceiverPosition: rawFlight.receiverPosition
          };
        })
      )
    )
  }

  getFlightsTmp(): Observable<Flight[]> {
    return this.http.get<Flight[]>('https://localhost:7088/flights');
  }

  private mapToFlight(rawFlight: any): Flight {
    return {
      FlarmId: rawFlight.flarmID,
      DisplayName: rawFlight.displayName,
      Registration: rawFlight.registration,
      Type: rawFlight.type,
      Model: rawFlight.model,
      Latitude: rawFlight.lat,
      Longitude: rawFlight.lng,
      HeightMSL: rawFlight.altitude,
      HeightAGL: rawFlight.agl,
      Timestamp: rawFlight.timestamp,
      Speed: rawFlight.speed,
      Vario: rawFlight.vario,
      VarioAverage: rawFlight.varioAverage,
      Receiver: rawFlight.receiver,
      ReceiverPosition: rawFlight.receiverPosition
    };
  }
}
