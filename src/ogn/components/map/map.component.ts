import { Component } from '@angular/core';
import * as Leaflet from 'leaflet'; 
import { OgnFlight } from 'src/ogn/models/ogn-flight.model';
import { OgnService } from 'src/ogn/services/ogn.service';

Leaflet.Icon.Default.imagePath = 'assets/';
@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss']
})
export class MapComponent {
  loxnCoordinates = { lat: 47.837, lng: 16.222 }

  map!: Leaflet.Map;
  markers: Leaflet.Marker[] = [];
  options = {
    layers: [
      Leaflet.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
      })
    ],
    zoom: 11,
    center: { lat: 47.8, lng: 16 }
  }

  constructor(private ognService: OgnService) {}

  fetchMarkers(): void {
    this.ognService.getOgnFlights().subscribe(
      data => {
        this.initMarkers(data);
      },
      error => {
        console.error('Error fetching flights:', error);
      }
    );
  }

  initMarkers(flights: OgnFlight[]) {
    const markers: any[] = []
    // Add flights to marker list
    flights.forEach(flight => {
      markers.push({
        data: flight,
        position: { 
          lat: flight.Latitude, 
          lng: flight.Longitude 
        }
      },)
    });
    for (let i = 0; i < markers.length; i++) {
      const markerData = markers[i];
      const marker = this.generateMarker(markerData, i);
      marker.addTo(this.map).bindPopup(`<b>${markerData.data.Registration} (${markerData.data.RegistrationShort})</b>`);
      this.markers.push(marker)
    }
  }

  generateMarker(markerData: any, index: number) {
    const icon = Leaflet.divIcon({
      //iconUrl: 'assets/glider.png',
      className: 'airplane-icon',
      html: `<img src="assets/glider.png" style="height: 80px; width: 80px; transform: rotate(${markerData.data.Direction}deg);" />`,
      iconSize: [80, 80],
      iconAnchor: [40, 40]
    });
    return Leaflet.marker(markerData.position, { icon: icon })
      .on('click', (event) => this.markerClicked(event, index))
  }

  onMapReady($event: Leaflet.Map) {
    this.map = $event;
    this.fetchMarkers();
  }

  mapClicked($event: any) {
    console.log($event.latlng.lat, $event.latlng.lng);
  }

  markerClicked($event: any, index: number) {
    console.log($event.latlng.lat, $event.latlng.lng);
  }
}
