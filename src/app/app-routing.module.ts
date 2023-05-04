import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FlightListComponent } from 'src/ogn/components/flight-list/flight-list.component';
import { MapComponent } from 'src/ogn/components/map/map.component';

const routes: Routes = [
  { 
    path: 'list', 
    component: FlightListComponent 
  },
  { 
    path: 'map', 
    component: MapComponent 
  },
  {
    path: '**',
    redirectTo: 'map'
  },
  {
    path: '',
    redirectTo: 'map',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
