import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MapComponent } from './components/map/map.component';
import { MatDialogModule } from '@angular/material/dialog';
import { MatRadioModule } from '@angular/material/radio';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { SettingsDialogComponent } from './components/settings-dialog/settings-dialog.component';
import { FormsModule } from '@angular/forms';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { InfoCardComponent } from './components/info-card/info-card.component';
import { MatIconModule } from '@angular/material/icon';
import { TimeAgoPipe } from './pipes/time-ago.pipe';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BarogramComponent } from './components/barogram/barogram.component';
import { NgChartsModule } from 'ng2-charts';
import GliderListComponent from './components/glider-list/glider-list.component';
import { MatSortModule } from '@angular/material/sort';

@NgModule({
  declarations: [
    MapComponent,
    SettingsDialogComponent,
    InfoCardComponent,
    TimeAgoPipe,
    BarogramComponent,
    GliderListComponent,
  ],
  imports: [
    CommonModule,
    MatTableModule,
    MatCardModule,
    MatDialogModule,
    MatRadioModule,
    MatCheckboxModule,
    BrowserAnimationsModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatTooltipModule,
    NgChartsModule,
    MatSortModule
  ],
  exports: [
    SettingsDialogComponent
  ]
})
export class OgnModule { }
