import { Injectable } from '@angular/core';
import Style from 'ol/style/Style';
import Stroke from 'ol/style/Stroke';
import { Flight } from '../models/flight.model';
import Icon from 'ol/style/Icon';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { takeUntil } from 'rxjs';
import { MapSettings } from '../models/map-settings.model';
import { GliderType } from '../models/glider-type';
import { MarkerColorScheme } from '../models/marker-color-scheme';

export const groundHeightBrown = '#947D6E';
export const groundHeightBackgroundBrown = '#A78D7C';
export const flightPathDarkRed = '#8B0000';
export const fligthPathDarkBlue = '#244485';
export const flightPathStrokeWhite = '#FFFFFFA0';

@Injectable({
  providedIn: 'root'
})
export class GliderMarkerService {
  flightPathStrokeStyle = new Style({
    stroke: new Stroke({
      color: flightPathStrokeWhite,
      width: 6,
    })
  });

  flightPathStyle = new Style({
    stroke: new Stroke({
      color: flightPathDarkRed,
      width: 2
    })
  });

  getGliderMarkerStyle(flight: Flight, settings: MapSettings, isSelected: boolean = false, isKnownGlider: boolean = false): Style {
    return new Style({
        image: new Icon({
          anchor: [0.5, 1],
          anchorXUnits: 'fraction',
          anchorYUnits: 'fraction',
          scale: 0.38,
          img: this.createLabelledGliderMarker(flight.displayName, settings, isSelected, isKnownGlider, flight.timestamp),
          imgSize: [88, 88]
        }),
    });
  }

  private createLabelledGliderMarker(
    label: string,
    settings: MapSettings,
    isSelected: boolean,
    isKnownGlider: boolean,
    lastUpdateTimestamp?: number
  ): HTMLCanvasElement {
    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d');

    let imageSource = 'assets/marker_blue.png';
    let textColor = 'white';

    if (isSelected) {
      imageSource = 'assets/marker_white.png';
      textColor = 'black'
    }
    else if (settings.gliderFilterOnMap === GliderType.all && !isKnownGlider && settings.markerColorScheme === MarkerColorScheme.highlightKnownGliders) {
      imageSource = 'assets/marker_yellow.png';
      textColor = 'black'
    }

    // Load the icon image
    const image = new Image();
    image.src = imageSource;

    // Calculate the opacity based on the lastUpdateTimestamp
    const minMinutes = 3;
    const maxMinutes = 20;
    const maxOpacity = 1;
    const minOpacity = 0.3;
    let opacity = maxOpacity;

    if (lastUpdateTimestamp) {
        const elapsedMinutes = (Date.now() - lastUpdateTimestamp) / 60000; // milliseconds to minutes
        if (elapsedMinutes > minMinutes) {
            let normalized = Math.min((elapsedMinutes - minMinutes) / (maxMinutes - minMinutes), 1);
            opacity = maxOpacity - normalized * (maxOpacity - minOpacity);
        }
    }

    // Wait for the image to load
    image.onload = () => {
        if (!context) {
            return;
        }
        // Draw the image on the canvas with calculated opacity
        context.globalAlpha = opacity;
        context.drawImage(image, 0, 0);

        // Set the font properties
        context.font = 'bold 26px Roboto';
        context.fillStyle = textColor;

        // Calculate the position for the text
        const textWidth = context.measureText(label).width;
        const x = (image.width - textWidth) / 2;
        const y = image.height / 2;

        // Draw the text on the canvas
        context.fillText(label, x, y);
    };

    return canvas;
  }
}
