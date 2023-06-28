import { Component } from '@angular/core';
import { MatTabChangeEvent } from '@angular/material/tabs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-bottom-navigation',
  templateUrl: './bottom-navigation.component.html',
  styleUrls: ['./bottom-navigation.component.scss']
})
export class BottomNavigationComponent {
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
