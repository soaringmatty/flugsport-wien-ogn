import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { OgnFlight } from '../models/ogn-flight.model';

@Injectable({
  providedIn: 'root'
})
export class OgnService {
  private flightsUrl = 'https://live.glidernet.org/lxml.php?a=0&b=48.72&c=46.65&d=16.70&e=11.00';
  private flightPathUrl = 'https://live.glidernet.org/livexml1.php?id=291d4598'

  constructor(private http: HttpClient) { }

  // Not used anymore -> Using own api instead
  getFlights(): Observable<OgnFlight[]> {
    const httpOptions = {
      headers: new HttpHeaders({ 'Content-Type': 'application/xml' }),
      responseType: 'text' as 'json'
    };
    return this.http.get(this.flightsUrl, httpOptions).pipe(
      map(response => {
        const parsedFlights = this.parseOgnFlightMarkers(response.toString());
        return parsedFlights.filter(flight => flight.Registration?.includes('-'));
      }),
      catchError(error => {
        console.error('Error fetching markers:', error);
        return throwError(error);
      })
    );
  }

  private parseOgnFlightMarkers(xmlString: string): OgnFlight[] {
    const parser = new DOMParser();
    const xmlDoc = parser.parseFromString(xmlString, 'application/xml');
    const markers = xmlDoc.getElementsByTagName('m');
    const ognFlights: OgnFlight[] = [];

    for (let i = 0; i < markers.length; i++) {
      const marker = markers[i];
      const attributes = marker.attributes;
      const ognFlight: OgnFlight = {};

      for (let j = 0; j < attributes.length; j++) {
        const attribute = attributes[j];
        if (attribute.name === 'a') {
          const [
            Latitude,
            Longitude,
            RegistrationShort,
            Registration,
            Height,
            LastUpdate,
            ElapsedSecondsSinceLastUpdate,
            Direction,
            GroundSpeed,
            VerticalSpeed,
            UnknownProperty,
            GroundStation,
            DeviceId,
            FlightId,
          ] = attribute.value.split(',');

          ognFlight.Latitude = parseFloat(Latitude);
          ognFlight.Longitude = parseFloat(Longitude);
          ognFlight.RegistrationShort = RegistrationShort;
          ognFlight.Registration = Registration;
          ognFlight.Height = parseInt(Height);
          ognFlight.LastUpdate = LastUpdate;
          ognFlight.ElapsedSecondsSinceLastUpdate = parseInt(ElapsedSecondsSinceLastUpdate);
          ognFlight.Direction = parseInt(Direction);
          ognFlight.GroundSpeed = parseFloat(GroundSpeed);
          ognFlight.VerticalSpeed = parseFloat(VerticalSpeed);
          ognFlight.UnknownProperty = parseFloat(UnknownProperty);
          ognFlight.GroundStation = GroundStation;
          ognFlight.DeviceId = DeviceId;
          ognFlight.FlightId = FlightId;
        }
      }

      ognFlights.push(ognFlight);
    }

    return ognFlights;
  }

  private parseFlightPath(xmlString: string): string {
    const parser = new DOMParser();
    const xmlDoc = parser.parseFromString(xmlString, 'application/xml');
    const markers = xmlDoc.getElementsByTagName('m');

    for (let i = 0; i < markers.length; i++) {
      const marker = markers[i];
      const attributes = marker.attributes;

      for (let j = 0; j < attributes.length; j++) {
        const attribute = attributes[j];
        if (attribute.name === 'r') {
          return attribute.value;
        }
      }
    }
    return '';
  }
}
