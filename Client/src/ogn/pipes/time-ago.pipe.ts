import { ChangeDetectorRef, OnDestroy, Pipe, PipeTransform } from '@angular/core';
import { Subscription } from 'rxjs';
import { ClockService } from '../services/clock.service';

@Pipe({
  name: 'timeAgo',
  pure: false
})
export class TimeAgoPipe implements PipeTransform, OnDestroy {
    private subscription!: Subscription;
    
    constructor(private clockService: ClockService, private changeDetectorRef: ChangeDetectorRef) {}

  transform(value: any, ...args: any[]): any {
    this.removeSubscription();
    this.subscription = this.clockService.getClock().subscribe(() => {
      this.changeDetectorRef.markForCheck();
    });
    if (value) {
      const seconds = Math.floor((+new Date() - +new Date(value)) / 1000);
      const intervals: { [key: string]: number } = {
        'Stunde': 3600,
        'Minute': 60,
        'Sekunde': 1
      };
      let counter;
      for (const i in intervals) {
        counter = Math.floor(seconds / intervals[i]);
        if (counter > 0)
          if (counter === 1) {
            return `vor ${counter} ${i}`; // singular (1 day ago)
          } else {
            return `vor ${counter} ${i}n`; // plural (2 days ago)
          }
      }
    }
    return value;
  }

  ngOnDestroy() {
    this.removeSubscription();
  }

  removeSubscription() {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
