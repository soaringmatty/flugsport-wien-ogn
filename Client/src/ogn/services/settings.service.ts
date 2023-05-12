import { Injectable } from '@angular/core';
import { MapSettings } from '../models/map-settings.model';
import { GliderType } from '../models/glider-type';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private settingsKey = 'mapSettings';
  private defaultSettings = {
    gliderFilter: GliderType.all,
    hideGlidersOnGround: false
  }

  constructor() { }

  saveSettings(settings: MapSettings): void {
    const settingsJson = JSON.stringify(settings);
    localStorage.setItem(this.settingsKey, settingsJson);
  }

  loadSettings(): MapSettings {
    const settingsJson = localStorage.getItem(this.settingsKey);
    if (settingsJson) {
      return JSON.parse(settingsJson);
    }
    return this.defaultSettings;
  }
}
