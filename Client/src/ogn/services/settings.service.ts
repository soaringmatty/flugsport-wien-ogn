import { Injectable } from '@angular/core';
import { MapSettings } from '../models/map-settings.model';
import { GliderType } from '../models/glider-type';
import { MapType } from '../models/map-type';

export const defaultSettings: MapSettings = {
  gliderFilterOnMap: GliderType.all,
  hideGlidersOnGround: false,
  mapType: MapType.osm,
  useFlightPathSmoothing: true,
  gliderFilterInLists: GliderType.club,
}

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private settingsKey = 'mapSettings';

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
    return defaultSettings;
  }
}
