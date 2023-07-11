import { Injectable } from '@angular/core';
import { MapSettings } from '../models/map-settings.model';
import { GliderType } from '../models/glider-type';
import { MapType } from '../models/map-type';
import config from '../../../package.json';

export const defaultSettings: MapSettings = {
  version: config.version,
  gliderFilterOnMap: GliderType.club,
  hideGlidersOnGround: false,
  mapType: MapType.stamen,
  useFlightPathSmoothing: true,
  gliderFilterInLists: GliderType.club,
  showChangelogForNewVersion: true,
  useExperimentalFeatures: false,
  useLowDataTransfer: false
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
      console.log('No stored settings')
      this.saveSettings(defaultSettings);
      return defaultSettings;
    }
    const loadedSettings = JSON.parse(settingsJson);
    if (config.version !== loadedSettings.version) {
      this.isNewVersion = true;
      console.log('Settings Version Mismatch', config.version, loadedSettings.version)
      console.log('Loaded Settings:', loadedSettings)
      console.log('Default Settings:', defaultSettings)
      const newSettings: any = {...defaultSettings};
      const newSettingsKeys = Object.keys(newSettings)
      newSettingsKeys.forEach(key => {
        const oldSettingsValue = loadedSettings[key];
        if (oldSettingsValue !== null && oldSettingsValue !== undefined && key !== 'version') {
          newSettings[key] = oldSettingsValue
        }
      });
      console.log('Overwritten Settings:', newSettings)
      this.saveSettings(newSettings);
      return newSettings;
    }
    console.log('No Settings Version Mismatch', loadedSettings);
    return loadedSettings;
  }
}
