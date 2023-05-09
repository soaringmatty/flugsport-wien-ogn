import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FlightListComponent } from './components/flight-list/flight-list.component';
import { MatTableModule } from '@angular/material/table';
import { MapComponent } from './components/map/map.component';

@NgModule({
  declarations: [
    FlightListComponent,
    MapComponent
  ],
  imports: [
    CommonModule,
    MatTableModule
  ]
})
export class OgnModule { }
