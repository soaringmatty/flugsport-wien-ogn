import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Flight } from 'src/ogn/models/flight.model';

@Component({
  selector: 'app-info-card',
  templateUrl: './info-card.component.html',
  styleUrls: ['./info-card.component.scss']
})
export class InfoCardComponent {
  @Input() flight: Flight | undefined
  @Output() close = new EventEmitter<void>();
  @Output() toggleActiveTracking = new EventEmitter<boolean>();
  @Output() toggleBarogram = new EventEmitter<boolean>();
  
  isTracking: boolean = false;
  showBarogram: boolean = false;

  closeDialog(): void {
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
