import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BarogramComponent } from 'src/ogn/components/barogram/barogram.component';
import GliderListComponent from 'src/ogn/components/glider-list/glider-list.component';
import { MapComponent } from 'src/ogn/components/map/map.component';
import { SettingsComponent } from 'src/ogn/components/settings/settings.component';

const routes: Routes = [
  { 
    path: 'map', 
    component: MapComponent 
  },
  { 
    path: 'list', 
    component: GliderListComponent 
  },
  { 
    path: 'barogram', 
    component: BarogramComponent 
  },
  { 
    path: 'settings', 
    component: SettingsComponent 
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
