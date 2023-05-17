import { Icon, Stroke, Style } from "ol/style";
import { GliderType } from "../models/glider-type";

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

export function getGliderMarkerStyle(label: string): Style {
    return new Style({
        image: new Icon({
          anchor: [0.5, 1],
          anchorXUnits: 'fraction',
          anchorYUnits: 'fraction',
          scale: 0.38,
          img: createLabelledGliderMarker(label),
          imgSize: [88, 88]
        }),
      });
}

export function createLabelledGliderMarker(label: string, isSelected: boolean = false, gliderType: GliderType = GliderType.all): HTMLCanvasElement {
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
    
  
    // Wait for the image to load
    image.onload = () => {
      if (!context) {
        return;
      }
      // Draw the image on the canvas
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