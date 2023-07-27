import { BreakpointObserver } from '@angular/cdk/layout';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { saveSettings } from 'src/app/store/app/app.actions';
import { mobileLayoutBreakpoints } from 'src/ogn/constants/layouts';
import { Flight } from 'src/ogn/models/flight.model';
import { MapSettings } from 'src/ogn/models/settings.model';

@Component({
  selector: 'app-info-card',
  templateUrl: './info-card.component.html',
  styleUrls: ['./info-card.component.scss']
})
export class InfoCardComponent implements OnInit {
  @Input() flight: Flight | undefined
  @Input() settings!: MapSettings
  @Output() close = new EventEmitter<void>();
  @Output() toggleActiveTracking = new EventEmitter<boolean>();
  @Output() toggleBarogram = new EventEmitter<boolean>();

  isMobilePortrait: boolean = false;
  isTracking: boolean = false;
  showBarogram: boolean = false;
  showOnlyLastFlight: boolean = false;

  constructor(
    private breakpointObserver: BreakpointObserver,
    private store: Store<State>) {
  }

  ngOnInit(): void {
    this.breakpointObserver.observe(mobileLayoutBreakpoints).subscribe(result => {
      this.isMobilePortrait = result.matches;
    });
  }

  closeDialog(): void {
    this.showBarogram = false;
    this.close.emit();
  }

  roundNumber(number: number): number {
    return Math.round(number);
  }

  roundVario(number: number): string {
    return number.toFixed(1);
  }

  toggleTracking(): void {
    this.isTracking = !this.isTracking
    this.toggleActiveTracking.emit(this.isTracking);
  }

  toggleBarogramVisibility(): void {
    this.showBarogram = !this.showBarogram
    this.toggleBarogram.emit(this.showBarogram);
  }

  toggleFlightPathLength(): void {
    this.showOnlyLastFlight = !this.showOnlyLastFlight;
    this.settings = {
      ...this.settings,
      onlyShowLastFlight: this.showOnlyLastFlight
    }
    this.save();
  }

  private save(): void {
    this.store.dispatch(saveSettings({settings: this.settings}));
  }
}
