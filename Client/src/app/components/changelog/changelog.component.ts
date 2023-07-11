import { Component } from '@angular/core';
import config from '../../../../package.json'

@Component({
  selector: 'app-changelog',
  templateUrl: './changelog.component.html',
  styleUrls: ['./changelog.component.scss']
})
export class ChangelogComponent {
  versionNumber = config.version;
}
