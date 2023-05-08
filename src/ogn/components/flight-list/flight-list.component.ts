import { Component, Input, OnInit } from '@angular/core';
import { OgnFlight } from 'src/ogn/models/ogn-flight.model';
import { OgnService } from 'src/ogn/services/ogn.service';

@Component({
  selector: 'app-flight-list',
  templateUrl: './flight-list.component.html',
  styleUrls: ['./flight-list.component.scss']
})
export class FlightListComponent implements OnInit {
  @Input() flights: OgnFlight[] = []
  displayedColumns: string[] = ['Registration', 'RegistrationShort', 'DeviceId', 'ElapsedSecondsSinceLastUpdate'];

  constructor(private ognService: OgnService) {}

  ngOnInit(): void {
    this.fetchMarkers();
  }

  fetchMarkers(): void {
    this.ognService.getFlights().subscribe(
      data => {
        this.flights = data;
      },
      error => {
        console.error('Error fetching flights:', error);
      }
    );
  }
}