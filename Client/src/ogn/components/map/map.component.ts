import { Component, OnDestroy, OnInit } from '@angular/core';
import * as Leaflet from 'leaflet';
import { OgnFlight } from 'src/ogn/models/ogn-flight.model';
import { OgnService } from 'src/ogn/services/ogn.service';
import TileLayer from 'ol/layer/Tile';
import OSM from 'ol/source/OSM';
import { fromLonLat } from 'ol/proj';
import {Point, LineString} from 'ol/geom';
import { Vector as VectorLayer } from 'ol/layer';
import VectorSource from 'ol/source/Vector';
import { Icon, Stroke, Style } from 'ol/style';
import Polyline from 'ol/format/Polyline';
import { Overlay, Map, View, Feature } from 'ol';
import { Subject, interval, takeUntil } from 'rxjs';
import { GlideAndSeekService } from 'src/ogn/services/glideandseek.service';

Leaflet.Icon.Default.imagePath = 'assets/';
@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss']
})
export class MapComponent implements OnInit, OnDestroy {
  map!: Map;
  glidersVectorLayer!: VectorLayer<VectorSource>;
  flightPathVectorLayer!: VectorLayer<VectorSource>;
  showGliderPath = false;

  // Configs
  updateGliderPositions = true; // Defines whether glider positions should be updated every few seconds
  updatePositionTimeout = 5000 // Defines the timeout between glider position updates in ms

  private readonly flightPathStyle = new Style({
    stroke: new Stroke({
      color: 'red',
      width: 2
    })
  });
  private readonly loxnCoordinates = [16.222, 47.837] // [Long, Lat]
  private readonly defaultCoordinates = [16, 47.8] // [Long, Lat]
  private readonly onDestroy$ = new Subject<void>();

  constructor(private ognService: OgnService,
              private glideAndSeekService: GlideAndSeekService) {}

  ngOnInit(): void {
    this.initializeMap();
    // Load glider positions on map
    if (this.updateGliderPositions) {
      this.updateGliderPositionsOnMap();
      this.initializeTimerForGliderPositionUpdates();
    }
    else {
      this.updateGliderPositionsOnMap();
    }
    // Test load glide and seek flights
    this.glideAndSeekService.getFlightsTmp().subscribe(
      flights => {
        console.log('flights', flights)
      },
      error => {
        console.error('Error fetching flights:', error);
      }
    );
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  updateGliderPositionsOnMap() {
    this.map.removeLayer(this.glidersVectorLayer)
    this.glidersVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    this.loadAndDrawGliderMarkers();
    this.map.addLayer(this.glidersVectorLayer);
  }

  loadAndDrawGliderMarkers(): void {
    this.ognService.getFlights().subscribe(
      ognFlights => {
        this.drawGliderMarkers(ognFlights);
        this.drawPlaneOverlays(ognFlights);
      },
      error => {
        console.error('Error fetching flights:', error);
      }
    );
  }

  // Doesn't work right now because of CORS issues
  loadAndDrawFlightPath(): void {
    this.ognService.getFlightPath().subscribe(
      encodedPath => {
        this.drawEncodedFlightPath(encodedPath)
      },
      error => {
        console.error('Error fetching flight path:', error);
      }
    );
  }

  drawGliderMarkers(flights: OgnFlight[]) {
    flights.forEach(flight => {
      if (!flight.Longitude || !flight.Latitude || !flight.Direction) {
        return
      }
      const gliderMarkersFeature = new Feature({
        geometry: new Point(fromLonLat([flight.Longitude, flight.Latitude]))
      });
      const iconStyle = new Style({
        image: new Icon({
          src: 'assets/glider.png',
          anchor: [0.5, 0.5],
          anchorXUnits: 'fraction',
          anchorYUnits: 'fraction',
          scale: 0.15,
          rotation: flight.Direction * (Math.PI / 180), // Convert degrees to radians
        }),
      });
      gliderMarkersFeature.setStyle(iconStyle);
      this.glidersVectorLayer.getSource()?.addFeature(gliderMarkersFeature);
    });
  }

  private initializeTimerForGliderPositionUpdates() {
    interval(this.updatePositionTimeout)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(() => {
        this.updateGliderPositionsOnMap();
      });
  }

  private drawEncodedFlightPath(encodedPath: string): void {
    const polylineFormat = new Polyline();
    const decodedFeature = polylineFormat.readFeature(encodedPath, {
      dataProjection: 'EPSG:4326',
      featureProjection: 'EPSG:900913'
    });
    const decodedGeometry = decodedFeature.getGeometry();
    const lineFeature = new Feature(decodedGeometry);
    const lineStyle = new Style({
      stroke: new Stroke({
        color: 'red',
        width: 2
      })
    });
    lineFeature.setStyle(lineStyle);
    this.flightPathVectorLayer.getSource()?.addFeature(lineFeature);
  }

  private drawPlaneOverlays(flights: OgnFlight[]): void {
    flights.forEach(flight => {
      if (!flight.Longitude || !flight.Latitude || !flight.Direction) {
        return
      }
      const customElement = document.createElement('div');
      customElement.innerHTML = `
        <div style="background-color: rgba(255, 255, 0, 1); padding: 5px; border-radius: 3px;">
          ${flight.RegistrationShort}
        </div>
      `;
      var marker = new Overlay({
        position: fromLonLat([flight.Longitude, flight.Latitude]),
        positioning: 'center-center',
        element: customElement,
        stopEvent: false
      });
      this.map.addOverlay(marker);
    });
  }

  // Not used at the moment
  private drawPathFromCoordinates(coordinates: [number, number][]): void {
    const projectedCoordinates = coordinates.map(coord => fromLonLat(coord));
    const lineString = new LineString(projectedCoordinates);
    const lineFeature = new Feature(lineString);
    const lineStyle = new Style({
      stroke: new Stroke({
        color: 'red',
        width: 2
      })
    });
    lineFeature.setStyle(lineStyle);
    this.flightPathVectorLayer.getSource()?.addFeature(lineFeature);
  }

  private initializeMap() {
    const osmTileLayer = new TileLayer({
      source: new OSM(),
    })
    this.glidersVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    this.flightPathVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    this.map = new Map({
      target: 'map',
      layers: [
        osmTileLayer,
        this.glidersVectorLayer,
        this.flightPathVectorLayer
      ],
      view: new View({
        center: fromLonLat(this.defaultCoordinates),
        zoom: 11
      })
    });
    // change mouse cursor when over marker
    this.map.on('pointermove', (e) => {
      const hit = this.map.hasFeatureAtPixel(e.pixel);
      this.map.getTargetElement().style.cursor = hit ? 'pointer' : '';
    });
  }
}
