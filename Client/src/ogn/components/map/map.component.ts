import { Component, OnDestroy, OnInit } from '@angular/core';
import * as Leaflet from 'leaflet';
import { Flight } from 'src/ogn/models/flight.model';
import { OgnService } from 'src/ogn/services/ogn.service';
import TileLayer from 'ol/layer/Tile';
import OSM from 'ol/source/OSM';
import { fromLonLat } from 'ol/proj';
import {Point, LineString} from 'ol/geom';
import { Vector as VectorLayer } from 'ol/layer';
import VectorSource from 'ol/source/Vector';
import { Fill, Icon, Stroke, Style, Text } from 'ol/style';
import Polyline from 'ol/format/Polyline';
import { Overlay, Map, View, Feature } from 'ol';
import { Subject, flatMap, interval, takeUntil } from 'rxjs';
import { ApiService } from 'src/ogn/services/api.service';

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

  constructor(private apiService: ApiService) {}

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
    this.apiService.getFlights().subscribe(
      flights => {
        this.drawGliderMarkers(flights);
      },
      error => {
        console.error('Error fetching flights:', error);
      }
    );
  }

  loadAndDrawFlightPath(flarmId: string): void {
    this.apiService.getFlightPath(flarmId).subscribe(
      encodedPath => {
        this.drawEncodedFlightPath(encodedPath)
      },
      error => {
        console.error('Error fetching flight path:', error);
      }
    );
  }

  drawGliderMarkers(flights: Flight[]) {
    flights.forEach(flight => {
      if (!flight.longitude || !flight.latitude) {
        return
      }
      const gliderMarkersFeature = new Feature({
        geometry: new Point(fromLonLat([flight.longitude, flight.latitude]))
      });
      const iconStyle = new Style({
        image: new Icon({
          anchor: [0.5, 1],
          anchorXUnits: 'fraction',
          anchorYUnits: 'fraction',
          src: 'assets/marker_yellow.png',
          scale: 0.4,
        }),
        text: new Text({
          text: flight.displayName,
          font: '12px Roboto',
          fill: new Fill({
            color: 'black',
          }),
          offsetY: -19
        }),
      })
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
    // change mouse cursor to "pointer" when hovering over marker
    this.map.on('pointermove', (e) => {
      const hit = this.map.hasFeatureAtPixel(e.pixel);
      this.map.getTargetElement().style.cursor = hit ? 'pointer' : '';
    });
  }
}
