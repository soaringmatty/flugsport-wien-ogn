import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Flight } from 'src/ogn/models/flight.model';

@Component({
  selector: 'app-info-card',
  templateUrl: './info-card.component.html',
  styleUrls: ['./info-card.component.scss']
})
export class InfoCardComponent implements OnInit {
  @Input() flight: Flight | undefined
  @Output() close = new EventEmitter<void>();
  @Output() toggleActiveTracking = new EventEmitter<boolean>();
  @Output() toggleBarogram = new EventEmitter<boolean>();
 
  isMobilePortrait: boolean = false;
  isTracking: boolean = false;
  showBarogram: boolean = false;

  constructor(private breakpointObserver: BreakpointObserver) {
  }

  ngOnInit(): void {
    this.breakpointObserver.observe([
      Breakpoints.HandsetPortrait
    ]).subscribe(result => {
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
}
