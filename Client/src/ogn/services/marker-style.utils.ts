import { Icon, Stroke, Style } from "ol/style";
import { GliderType } from "../models/glider-type";
import { Flight } from "../models/flight.model";

export const flightPathDarkRed = '#8B0000';
export const fligthPathDarkBlue = '#244485';
export const flightPathStrokeWhite = '#FFFFFFA0';
export const groundHeightBrown = '#947D6E';
export const groundHeightBackgroundBrown = '#A78D7C';

export const flightPathStrokeStyle = new Style({
  stroke: new Stroke({
    color: flightPathStrokeWhite,
    width: 6,
  })
});

export const flightPathStyle = new Style({
  stroke: new Stroke({
    color: flightPathDarkRed,
    width: 2
  })
});

export function getGliderMarkerStyle(flight: Flight): Style {
    return new Style({
        image: new Icon({
          anchor: [0.5, 1],
          anchorXUnits: 'fraction',
          anchorYUnits: 'fraction',
          scale: 0.38,
          img: createLabelledGliderMarker(flight.displayName, false, GliderType.all, flight.timestamp),
          imgSize: [88, 88]
        }),
      });
}

export function createLabelledGliderMarker(
  label: string, 
  isSelected: boolean = false, 
  gliderType: GliderType = GliderType.all, 
  lastUpdateTimestamp?: number
): HTMLCanvasElement {
  const canvas = document.createElement('canvas');
  const context = canvas.getContext('2d');

  // Load the icon image
  const image = new Image();
  if (isSelected) {
      image.src = 'assets/marker_white.png'
  }
  else {
      image.src = 'assets/marker_blue.png';
  }

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
      context.fillStyle = 'white';

      // Calculate the position for the text
      const textWidth = context.measureText(label).width;
      const x = (image.width - textWidth) / 2;
      const y = image.height / 2;

      // Draw the text on the canvas
      context.fillText(label, x, y);
  };

  return canvas;
}