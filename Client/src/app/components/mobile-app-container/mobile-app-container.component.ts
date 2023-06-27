import { Component } from '@angular/core';
import { MatTabChangeEvent } from '@angular/material/tabs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-mobile-app-container',
  templateUrl: './mobile-app-container.component.html',
  styleUrls: ['./mobile-app-container.component.scss']
})
export class MobileAppContainerComponent {
  constructor(private router: Router) { }

  onTabChanged(event: MatTabChangeEvent) {
    switch (event.index) {
      case 0: // index of 'Karte' tab
        this.router.navigateByUrl('/map'); // update this with your route
        break;
      case 1: // index of 'Liste' tab
        this.router.navigateByUrl('/list'); // update this with your route
        break;
      case 2: // index of 'Einstellungen' tab
        //this.router.navigateByUrl('/settings'); // update this with your route
        break;
      default:
        break;
    }
  }
}
