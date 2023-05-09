import { Component, OnInit } from '@angular/core';
import { OgnService } from 'src/ogn/services/ogn.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  markers: any[] = [];

  constructor(private ognService: OgnService) {}

  ngOnInit(): void {
    this.fetchMarkers();
  }

  fetchMarkers(): void {
    this.ognService.getFlights().subscribe(
      data => {
        this.markers = data;
        console.log('Markers:', this.markers);
      },
      error => {
        console.error('Error fetching markers:', error);
      }
    );
  }
}
