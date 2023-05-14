import { Component, OnDestroy, OnInit } from '@angular/core';
import { Flight } from 'src/ogn/models/flight.model';
import { OSM, Vector as VectorSource } from 'ol/source';
import { fromLonLat, toLonLat } from 'ol/proj';
import { Point } from 'ol/geom';
import { Vector as VectorLayer, Tile as TileLayer } from 'ol/layer';
import { Icon, Style } from 'ol/style';
import Polyline from 'ol/format/Polyline';
import { Map, View, Feature } from 'ol';
import { Subject, Subscription, interval, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { loadFlightPath, loadFlights } from 'src/app/store/app/app.actions';
import { createLabelledGliderMarker, flightPathStyle, flightPathStrokeStyle, getGliderMarkerStyle } from 'src/ogn/services/marker-style.utils';
import { LineString } from 'ol/geom'
import { Coordinate } from 'ol/coordinate';

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss']
})
export class MapComponent implements OnInit, OnDestroy {
  map!: Map;
  glidersVectorLayer!: VectorLayer<VectorSource>;
  flightPathStrokeVectorLayer!: VectorLayer<VectorSource>;
  flightPathVectorLayer!: VectorLayer<VectorSource>;
  showGliderPath = false;
  flights: Flight[] = [];
  selectedFlight: Flight | undefined;
  isTracking: boolean = false;
  trackingSubscription!: Subscription;
  mapZoomBeforeActiveTracking: number | undefined;
  mapCenterBeforeActiveTracking: Coordinate | undefined;
  lastHistoricalPosition: Coordinate | undefined;

  // Configs
  updateGliderPositions = true; // Defines whether glider positions should be updated every few seconds
  updatePositionTimeout = 5000 // Defines the timeout between glider position updates in ms
  
  private readonly loxnCoordinates = [16.222, 47.837] // [Long, Lat]
  private readonly defaultCoordinates = [16, 47.8] // [Long, Lat]
  private readonly onDestroy$ = new Subject<void>();

  constructor(private store: Store<State>) {
  }

  ngOnInit(): void {
    this.initializeMap();
    // Redraw markers on map on every update
    this.store.select(x => x.app.flights).pipe(
      takeUntil(this.onDestroy$)
    ).subscribe(flights => {
      this.flights = flights;
      this.updateGliderPositionsOnMap(flights);
      // If a glider is selected, update flight info and flight path
      const updatedSelectedFlight = this.flights.find(x => x.flarmId === this.selectedFlight?.flarmId);
      if (this.selectedFlight && updatedSelectedFlight) {
        // this.drawFlightPathExtension(
        //   [this.selectedFlight.longitude, this.selectedFlight.latitude], 
        //   [updatedSelectedFlight.longitude, updatedSelectedFlight.latitude]
        //   )
        this.selectedFlight = updatedSelectedFlight;
      }
    })
    // Draw flight path on map when loaded
    this.store.select(x => x.app.flightPath).pipe(
      takeUntil(this.onDestroy$)
    ).subscribe(encodedFlightPath => {
      this.drawEncodedFlightPath(encodedFlightPath);
    })
    // Load and draw glider positions on map
    if (this.updateGliderPositions) {
      this.store.dispatch(loadFlights());
      this.initializeTimerForGliderPositionUpdates();
    }
    else {
      this.store.dispatch(loadFlights());
    }
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  toggleActiveTracking(newIsTracking: boolean): void {
    if (newIsTracking) {
      this.startActiveTracking();
    }
    else {
      this.stopActiveTracking();
    }
    this.isTracking = newIsTracking;
  }

  private startActiveTracking(): void {
    if (!this.selectedFlight) {
      console.warn('Unable to start active tracking. No glider selected')
      return;
    }
    this.isTracking = true;
    this.mapZoomBeforeActiveTracking = this.map.getView().getZoom();
    this.mapCenterBeforeActiveTracking = this.map.getView().getCenter();
    const flight = this.flights.find(x => x.flarmId === this.selectedFlight?.flarmId);
    if (flight && flight.longitude && flight.latitude) {
      const coordinate = fromLonLat([flight.longitude, flight.latitude]);
      this.map.getView().setCenter(coordinate);
      this.map.getView().setZoom(15);

      // Subscribe to position updates
      this.trackingSubscription = this.store.select(x => x.app.flights).subscribe(flights => {
        const updatedFlight = flights.find(x => x.flarmId === this.selectedFlight?.flarmId);
        if (updatedFlight && updatedFlight.longitude && updatedFlight.latitude) {
          const updatedCoordinate = fromLonLat([updatedFlight.longitude, updatedFlight.latitude]);
          this.map.getView().setCenter(updatedCoordinate);
        }
      })
    }
  }

  private stopActiveTracking(): void {
    this.isTracking = false;
    this.trackingSubscription?.unsubscribe();
    if (this.mapZoomBeforeActiveTracking && this.mapCenterBeforeActiveTracking) {
      this.map.getView().setZoom(this.mapZoomBeforeActiveTracking);
      this.map.getView().setCenter(this.mapCenterBeforeActiveTracking);
    }
    
  }

  unselectGlider(): void {
    this.selectedFlight = undefined;
    this.flightPathStrokeVectorLayer.getSource()?.clear();
  }

  private selectGlider(flarmId: string): void {
    const flight = this.flights.find(x => x.flarmId === flarmId);
    if (flight) {
      this.selectedFlight = flight;
    }
  }

  private updateGliderPositionsOnMap(flights: Flight[]) {
    flights.forEach(flight => {
      if (!flight.longitude || !flight.latitude) {
        return;
      }
      const existingFeature = this.glidersVectorLayer.getSource()?.getFeatureById(flight.flarmId);
      // If marker already exists -> just update it's position
      if (existingFeature) {
        existingFeature.setGeometry(new Point(fromLonLat([flight.longitude, flight.latitude])));
      }
      // If marker does not exist -> create new marker
      else {
        const gliderMarkersFeature = new Feature({
          geometry: new Point(fromLonLat([flight.longitude, flight.latitude])),
          flarmId: flight.flarmId
        });
        gliderMarkersFeature.setId(flight.flarmId);
        const iconStyle = getGliderMarkerStyle(flight.displayName);
        gliderMarkersFeature.setStyle(iconStyle);
        this.glidersVectorLayer.getSource()?.addFeature(gliderMarkersFeature);
      }
    });
    // Remove features that no longer exist in flights
    this.glidersVectorLayer.getSource()?.getFeatures().forEach(feature => {
      const flarmId = feature.getId();
      if (!flights.find(flight => flight.flarmId === flarmId)) {
        this.glidersVectorLayer.getSource()?.removeFeature(feature);
      }
    });
  }

  private initializeTimerForGliderPositionUpdates() {
    interval(this.updatePositionTimeout)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(() => {
        this.store.dispatch(loadFlights());
        if (this.selectedFlight) {
          this.store.dispatch(loadFlightPath({flarmId: this.selectedFlight.flarmId}));
        }
      });
  }

  private drawEncodedFlightPath(encodedPath: string): void {
    const polylineFormat = new Polyline();
    const decodedFeature = polylineFormat.readFeature(encodedPath, {
      dataProjection: 'EPSG:4326',
      featureProjection: 'EPSG:900913'
    });
    const decodedGeometry = decodedFeature.getGeometry() as LineString;
    const coordinates = decodedGeometry.getCoordinates();
    const smoothedCoords: Coordinate[] = this.chaikinsAlgorithm(coordinates);
    if (!smoothedCoords?.length) {
      return;
    }

    const outerLineFeature = new Feature(new LineString(smoothedCoords));
    outerLineFeature.setStyle(flightPathStrokeStyle);
    const innerLineFeature = new Feature(new LineString(smoothedCoords));
    innerLineFeature.setStyle(flightPathStyle);

    // Clear the previous flight path before drawing the new one
    this.flightPathStrokeVectorLayer.getSource()?.clear();
    this.flightPathVectorLayer.getSource()?.clear();
    this.flightPathStrokeVectorLayer.getSource()?.addFeature(outerLineFeature);
    this.flightPathVectorLayer.getSource()?.addFeature(innerLineFeature);
  }

  chaikinsAlgorithm(coords: Coordinate[], iterations: number = 3, factor: number = 0.2): Coordinate[] {
    let result = coords;
  
    for (let i = 0; i < iterations; i++) {
      let smoothedCoords: Coordinate[] = [];
  
      for (let i = 0; i < result.length - 1; i++) {
        const p0 = result[i];
        const p1 = result[i + 1];
  
        const Q: Coordinate = [(1 - factor) * p0[0] + factor * p1[0], (1 - factor) * p0[1] + factor * p1[1]];
        const R: Coordinate = [factor * p0[0] + (1 - factor) * p1[0], factor * p0[1] + (1 - factor) * p1[1]];
  
        smoothedCoords.push(Q);
        smoothedCoords.push(R);
      }
  
      result = smoothedCoords;
    }
  
    return result;
  }
  
  private drawFlightPathExtension(start: [number, number], end: [number, number]): void {
    const startProj = fromLonLat(start);
    const endProj = fromLonLat(end);
  
    const lineString = new LineString([this.lastHistoricalPosition as Coordinate, startProj, endProj]);

    const coordinates = lineString.getCoordinates();
    const smoothedCoords: Coordinate[] = this.chaikinsAlgorithm(coordinates);
    if (!smoothedCoords?.length) {
      return;
    }
    
    const outerLineFeature = new Feature(new LineString(smoothedCoords));
    outerLineFeature.setStyle(flightPathStrokeStyle);
    const innerLineFeature = new Feature(new LineString(smoothedCoords));
    innerLineFeature.setStyle(flightPathStyle);

    // Clear the previous flight path before drawing the new one
    this.flightPathStrokeVectorLayer.getSource()?.addFeature(outerLineFeature);
    this.flightPathVectorLayer.getSource()?.addFeature(innerLineFeature);

    this.lastHistoricalPosition = startProj;
  }

  private initializeMap() {
    // Load the stored map state or use the default values
    const storedCenter = sessionStorage.getItem('mapCenter');
    const storedZoom = sessionStorage.getItem('mapZoom');
    const initialCenter = storedCenter ? JSON.parse(storedCenter) : this.defaultCoordinates;
    const initialZoom = storedZoom ? +storedZoom : 11;
  
    const osmTileLayer = new TileLayer({
      source: new OSM(),
    });
    this.glidersVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    this.flightPathStrokeVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    this.flightPathVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    const mapView = new View({
      center: fromLonLat(initialCenter),
      zoom: initialZoom,
    })
    // Always store current map center and zoom in session storage
    mapView.on('change', (event) => {
      const zoom = this.map.getView().getZoom();
      if (zoom !== undefined) {
        sessionStorage.setItem('mapZoom', zoom.toString());
      }
      const center = this.map.getView().getCenter();
      if (center) {
        sessionStorage.setItem('mapCenter', JSON.stringify(toLonLat(center)));
      }
    });
    
    this.map = new Map({
      target: 'map',
      layers: [
        osmTileLayer,
        this.flightPathStrokeVectorLayer,
        this.flightPathVectorLayer,
        this.glidersVectorLayer,
      ],
      view: mapView
    });

    // change mouse cursor to "pointer" when hovering over marker
    this.map.on('pointermove', (e) => {
      const hit = this.map.hasFeatureAtPixel(e.pixel);
      this.map.getTargetElement().style.cursor = hit ? 'pointer' : '';
    });
    // Show information card and load flight path when marker is clicked
    this.map.on('singleclick', (e) => {
      const features = this.map.getFeaturesAtPixel(e.pixel);
      if (!features || features.length < 1) {
        return;
      }
      const feature = features[0];
      const flarmId = feature.get('flarmId');
      if (flarmId) {
        this.selectGlider(flarmId);
        this.store.dispatch(loadFlightPath({flarmId}));
      } 
    });
  }
}
