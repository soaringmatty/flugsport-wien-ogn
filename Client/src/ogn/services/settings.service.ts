import { Injectable } from '@angular/core';
import { MapSettings } from '../models/map-settings.model';
import { GliderType } from '../models/glider-type';
import { MapType } from '../models/map-type';
import config from '../../../package.json';
import { MarkerColorScheme } from '../models/marker-color-scheme';

export const defaultSettings: MapSettings = {
  version: config.version,
  gliderFilterOnMap: GliderType.club,
  hideGlidersOnGround: false,
  showGlidersOnly: false,
  mapType: MapType.osm,
  useFlightPathSmoothing: true,
  onlyShowLastFlight: false,
  gliderFilterInLists: GliderType.club,
  showChangelogForNewVersion: true,
  markerColorScheme: MarkerColorScheme.highlightKnownGliders,
  updateTimeout: 5000,
  useUtcTimeInDepartureList: true,
}

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  isNewVersion = false;
  private settingsKey = 'mapSettings';

  constructor() { }

  saveSettings(settings: MapSettings): void {
    const settingsJson = JSON.stringify(settings);
    localStorage.setItem(this.settingsKey, settingsJson);
  }

  loadSettings(): MapSettings {
    const settingsJson = localStorage.getItem(this.settingsKey);
    if (!settingsJson) {
      this.saveSettings(defaultSettings);
      return defaultSettings;
    }
    const loadedSettings = JSON.parse(settingsJson);
    if (config.version !== loadedSettings.version) {
      this.isNewVersion = true;
      const newSettings: any = {...defaultSettings};
      const newSettingsKeys = Object.keys(newSettings)
      newSettingsKeys.forEach(key => {
        const oldSettingsValue = loadedSettings[key];
        if (oldSettingsValue !== null && oldSettingsValue !== undefined && key !== 'version') {
          newSettings[key] = oldSettingsValue
        }
      });
      this.saveSettings(newSettings);
      return newSettings;
    }
    return loadedSettings;
  }
}
