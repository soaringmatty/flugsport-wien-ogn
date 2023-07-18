import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { OgnModule } from 'src/ogn/ogn.module';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { HttpClientModule } from '@angular/common/http';
import { EffectsModule } from '@ngrx/effects';
import { StoreModule } from '@ngrx/store';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';
import { reducers, metaReducers } from './store';
import { AppEffects } from './store/app/app.effects';
import { MatDialogModule } from '@angular/material/dialog';
import { NgChartsModule } from 'ng2-charts';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { BottomNavigationComponent } from './components/bottom-navigation/bottom-navigation.component'
import { MatCardModule } from '@angular/material/card';
import { ChangelogComponent } from './components/changelog/changelog.component';
import { OrientationNotSupportedComponent } from './components/orientation-not-supported/orientation-not-supported.component';

@NgModule({
  declarations: [
    AppComponent,
    BottomNavigationComponent,
    ChangelogComponent,
    OrientationNotSupportedComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    OgnModule,
    BrowserAnimationsModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    HttpClientModule,
    StoreModule.forRoot(reducers, { metaReducers }),
    StoreDevtoolsModule.instrument(),
    EffectsModule.forRoot([AppEffects]),
    MatDialogModule,
    NgChartsModule,
    MatSnackBarModule,
    MatTabsModule,
    MatCardModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
