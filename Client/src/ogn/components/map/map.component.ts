import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Flight } from 'src/ogn/models/flight.model';
import {
  OSM,
  Vector as VectorSource,
  Tile as TileSource,
  Stamen
} from 'ol/source';
import { fromLonLat, toLonLat, transformExtent } from 'ol/proj';
import { Point } from 'ol/geom';
import { Vector as VectorLayer, Tile as TileLayer } from 'ol/layer';
import Polyline from 'ol/format/Polyline';
import { Map as OlMap, View, Feature } from 'ol';
import { Subject, Subscription, interval, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { loadFlightHistory, loadFlightPath, loadFlights, selectFlight } from 'src/app/store/app/app.actions';
import { LineString } from 'ol/geom';
import { Coordinate } from 'ol/coordinate';
import { MapSettings } from 'src/ogn/models/map-settings.model';
import { BarogramComponent } from '../barogram/barogram.component';
import { HistoryEntry } from 'src/ogn/models/history-entry.model';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { coordinates } from 'src/ogn/constants/coordinates';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MapType } from 'src/ogn/models/map-type';
import { clubGliders, getClubAndPrivateGliders, privateGliders } from 'src/ogn/constants/known-gliders';
import { GliderMarkerProperties, GliderMarkerService } from 'src/ogn/services/glider-marker.service';
import { FlightAnalysationService } from 'src/ogn/services/flight-analysation.service';
import { GliderType } from 'src/ogn/models/glider-type';
import { mobileLayoutBreakpoints } from 'src/ogn/constants/layouts';
import { MapBarogramSyncService, MarkerLocationUpdate } from 'src/ogn/services/map-barogram-sync.service';
import Style from 'ol/style/Style';
import Icon from 'ol/style/Icon';

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss'],
})
export class MapComponent implements OnInit, OnDestroy {
  @ViewChild(BarogramComponent, { static: false }) barogram!: BarogramComponent;

  map!: OlMap;
  glidersVectorLayer!: VectorLayer<VectorSource>;
  flightPathStrokeVectorLayer!: VectorLayer<VectorSource>;
  flightPathVectorLayer!: VectorLayer<VectorSource>;
  flightPathMarkerVectorLayer!: VectorLayer<VectorSource>;
  backgroundTileLayer!: TileLayer<TileSource>
  flights: Flight[] = [];
  selectedFlight: Flight | undefined;
  previousSelectedFlight: Flight | undefined;
  settings!: MapSettings;
  showBarogram: boolean = false;
  isMobilePortrait: boolean = false;
  // Tracking related properties
  isTracking: boolean = false;
  trackingSubscription!: Subscription;
  mapZoomBeforeActiveTracking: number | undefined;
  mapCenterBeforeActiveTracking: Coordinate | undefined;

  private markerDictionary: Map<string, GliderMarkerProperties> = new Map();
  private readonly onDestroy$ = new Subject<void>();

  constructor(
    private store: Store<State>,
    private route: ActivatedRoute,
    private breakpointObserver: BreakpointObserver,
    private gliderMarkerService: GliderMarkerService,
    private flightAnalysationService: FlightAnalysationService,
    private mapBarogramSyncService: MapBarogramSyncService
  ) {}

  ngOnInit(): void {
    this.initializeMap();
    this.breakpointObserver.observe(mobileLayoutBreakpoints).subscribe(result => {
      this.isMobilePortrait = result.matches;
    });
    // Update markers on map every time the flights are loaded
    this.store
      .select((x) => x.app.flights)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((flights) => {
        this.flights = flights;
        if (this.settings) {
          this.updateGliderPositionsOnMap(flights);
        }
      });
    // Draw flight path on map every time the flight history data in store is updated
    this.store
      .select((x) => x.app.flightHistory)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((history) => {
        if (this.selectedFlight) {
          this.drawFlightPathFromHistory(history);
        }
      });
    // Refresh data when settings are updated
    this.store
      .select((x) => x.app.settings)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(settings => {
        if (this.settings) {
          this.settings = settings;
          this.setMapTilesAccordingToSettings();
          this.refreshData();
          return;
        }
        this.settings = settings
        this.setMapTilesAccordingToSettings();
      });
    // Subscribe to selected flight in store
    this.store
      .select((x) => x.app.selectedFlight)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(selectedFlight => this.selectedFlight = selectedFlight ? selectedFlight : undefined);

    // Sync marker on map with selected timestamp in barogram
    this.mapBarogramSyncService.markerLocationUpdateRequested
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((markerLocationUpdate) => {
        this.drawMarkerOnFlightPath(markerLocationUpdate);
      })

    // Set map center and zoom level based on route params
    this.route.paramMap.pipe(takeUntil(this.onDestroy$)).subscribe((params: ParamMap) => {
      if (params) {
        const lat = params.get('lat');
        const lon = params.get('lon');
        if (lat && lon) {
          const coordinate = fromLonLat([parseFloat(lon), parseFloat(lat)]);
          this.map.getView().setCenter(coordinate);
          this.map.getView().setZoom(13);
        }
      }
    });

    this.initiallyLoadData();
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  toggleActiveTracking(newIsTracking: boolean): void {
    if (newIsTracking) {
      this.startActiveTracking();
    } else {
      this.stopActiveTracking();
    }
    this.isTracking = newIsTracking;
  }

  toggleBarogram(newShowBarogram: boolean): void {
    this.showBarogram = newShowBarogram;
    if (!newShowBarogram) {
      // Clear location marker on map
      this.mapBarogramSyncService.updateLocationMarkerOnMap();
    }
  }

  selectGlider(flarmId: string): void {
    if (this.selectedFlight) {
      this.unselectGlider();
    }
    const flight = this.flights.find((x) => x.flarmId === flarmId);
    if (flight) {
      this.store.dispatch(selectFlight({flight}))
      const gliderType = this.gliderMarkerService.getGliderType(flarmId);
      this.updateSingleMarkerOnMap(flight, gliderType)
      this.store.dispatch(loadFlightHistory({ flarmId }));
    }
  }

  unselectGlider(): void {
    this.previousSelectedFlight = this.selectedFlight;
    this.store.dispatch(selectFlight({flight: null}))
    this.stopActiveTracking();
    this.showBarogram = false;
    const gliderType = this.gliderMarkerService.getGliderType(this.previousSelectedFlight?.flarmId);
    this.updateSingleMarkerOnMap(this.previousSelectedFlight as Flight, gliderType)
    this.flightPathStrokeVectorLayer.getSource()?.clear();
    this.flightPathVectorLayer.getSource()?.clear();
    this.flightPathMarkerVectorLayer.getSource()?.clear();
  }

 // Initially load and draw glider positions on map
  private initiallyLoadData(): void {
    document.fonts.load('bold 26px Roboto').then(() => {
      this.refreshData();
      this.setupTimerForGliderPositionUpdates();
    });
  }

  // Refresh plane positions, flight data and flight path (if a glider is selected)
  private refreshData(): void {
    this.loadFlightsWithParams();
    if (this.selectedFlight) {
      this.store.dispatch(loadFlightHistory({flarmId: this.selectedFlight.flarmId}))
    }
  }

  private setMapTilesAccordingToSettings(): void {
    let source = new Stamen({layer: 'terrain'});
    switch (this.settings.mapType) {
      case MapType.osm:
        source = new OSM()
        break;
      case MapType.satellite:
      default:
        break;
    }
    this.backgroundTileLayer.setSource(source);
  }

  // Start to live track the selected gliders position (always keep map centered on glider)
  private startActiveTracking(): void {
    if (!this.selectedFlight) {
      console.warn('Unable to start active tracking. No glider selected');
      return;
    }
    this.isTracking = true;
    this.mapZoomBeforeActiveTracking = this.map.getView().getZoom();
    this.mapCenterBeforeActiveTracking = this.map.getView().getCenter();
    const flight = this.flights.find(
      (x) => x.flarmId === this.selectedFlight?.flarmId
    );
    if (flight && flight.longitude && flight.latitude) {
      const coordinate = fromLonLat([flight.longitude, flight.latitude]);
      this.map.getView().setCenter(coordinate);
      this.map.getView().setZoom(this.isMobilePortrait ? 13 : 15);

      // Subscribe to position updates
      this.trackingSubscription = this.store
        .select((x) => x.app.flights)
        .subscribe((flights) => {
          const updatedFlight = flights.find(
            (x) => x.flarmId === this.selectedFlight?.flarmId
          );
          if (
            updatedFlight &&
            updatedFlight.longitude &&
            updatedFlight.latitude
          ) {
            const updatedCoordinate = fromLonLat([
              updatedFlight.longitude,
              updatedFlight.latitude,
            ]);
            this.map.getView().setCenter(updatedCoordinate);
          }
        });
    }
  }

  // Stop live tracking selected glider and restore map state before starting live tracking
  private stopActiveTracking(): void {
    this.isTracking = false;
    this.trackingSubscription?.unsubscribe();
    if (
      this.mapZoomBeforeActiveTracking &&
      this.mapCenterBeforeActiveTracking
    ) {
      this.map.getView().setZoom(this.mapZoomBeforeActiveTracking);
      this.map.getView().setCenter(this.mapCenterBeforeActiveTracking);
    }
  }

  // Draw glider markers on map (update marker position if marker already exists)
  private updateGliderPositionsOnMap (flights: Flight[]) {
    flights.forEach(flight => {
      const gliderType = this.gliderMarkerService.getGliderType(flight.flarmId);
      this.updateSingleMarkerOnMap(flight, gliderType)
    });

    // Remove features that no longer exist in flights
    this.glidersVectorLayer.getSource()?.getFeatures()
      .forEach((feature) => {
        const flarmId = feature.getId();
        const flightStillExists = flights.find((flight) => flight.flarmId === flarmId)
        if (!flightStillExists) {
          this.glidersVectorLayer.getSource()?.removeFeature(feature);
        }
      }
    );

  }

  private updateSingleMarkerOnMap (flight: Flight, gliderType: GliderType) {
      if (!flight.longitude || !flight.latitude) {
        return;
      }
      const existingFeature = this.glidersVectorLayer.getSource()?.getFeatureById(flight.flarmId);
      // If marker already exists and is selected or just got unseleted -> update it's position and style
      // If marker already exists and has no selection event -> just update it's position
      if (existingFeature) {
        existingFeature.setGeometry(
          new Point(fromLonLat([flight.longitude, flight.latitude]))
        );
        const shouldUpdateStyle = this.doesMarkerNeedStyleUpdate(flight.flarmId);
        if (shouldUpdateStyle) {
          const isSelected = this.selectedFlight?.flarmId === flight.flarmId;
          const iconStyle = this.gliderMarkerService.getGliderMarkerStyle(flight, this.settings, isSelected, gliderType);
          existingFeature.setGeometry(
            new Point(fromLonLat([flight.longitude, flight.latitude]))
          );
          existingFeature.setStyle(iconStyle);
        }
      }
      // If marker does not exist -> create new marker
      else {
        const gliderMarkerFeature = new Feature({
          geometry: new Point(fromLonLat([flight.longitude, flight.latitude])),
          flarmId: flight.flarmId,
        });
        gliderMarkerFeature.setId(flight.flarmId);
        const isSelected = this.selectedFlight?.flarmId === flight.flarmId;
        const iconStyle = this.gliderMarkerService.getGliderMarkerStyle(flight, this.settings, isSelected, gliderType);
        gliderMarkerFeature.setStyle(iconStyle);
        this.glidersVectorLayer.getSource()?.addFeature(gliderMarkerFeature);
      }
      this.updateMarkerInDictionary(flight.flarmId);
  }

  private doesMarkerNeedStyleUpdate(flarmId: string): boolean {
    const lastProperties = this.markerDictionary.get(flarmId);
    if (!lastProperties) {
      return true;
    }
    const isFlightSelected = this.selectedFlight?.flarmId === flarmId;
    if (lastProperties.isSelected !== isFlightSelected) {
      return true;
    }
    return false;
  }

  private updateMarkerInDictionary(flarmId: string): void {
    const lastProperties = this.markerDictionary.get(flarmId);
    let newProperties: GliderMarkerProperties = {
      isSelected: this.selectedFlight?.flarmId === flarmId
    }
    if (lastProperties) {
      newProperties = {
        ...lastProperties,
        isSelected: this.selectedFlight?.flarmId === flarmId
      }
    }
    this.markerDictionary.set(flarmId, newProperties);
  }

  private drawFlightPathFromHistory(historyEntries: HistoryEntry[]): void {
    let coordinates: Coordinate[] = []
    if (this.settings.onlyShowLastFlight) {
      const filteredHistoryEntries = this.flightAnalysationService.getHistorySinceLastTakeoff(historyEntries);
        coordinates = filteredHistoryEntries.map(entry => {
          return fromLonLat([entry.longitude, entry.latitude]);
        });
    }
    else {
      coordinates = historyEntries.map(entry => {
        return fromLonLat([entry.longitude, entry.latitude]);
      });
    }
    let geometry = new LineString(coordinates);
    if (this.settings.useFlightPathSmoothing) {
      const smoothedCoords: Coordinate[] = this.chaikinsAlgorithm(coordinates);
      if (!smoothedCoords?.length) {
        return;
      }
      geometry = new LineString(smoothedCoords);
    }

    const outerLineFeature = new Feature(geometry);
    const innerLineFeature = new Feature(geometry);
    outerLineFeature.setStyle(this.gliderMarkerService.flightPathStrokeStyle);
    innerLineFeature.setStyle(this.gliderMarkerService.flightPathStyle);

    this.flightPathStrokeVectorLayer.getSource()?.clear();
    this.flightPathStrokeVectorLayer.getSource()?.addFeature(outerLineFeature);
    this.flightPathVectorLayer.getSource()?.clear();
    this.flightPathVectorLayer.getSource()?.addFeature(innerLineFeature);
  }

  private drawMarkerOnFlightPath(markerLocationUpdate: MarkerLocationUpdate) {
    this.flightPathMarkerVectorLayer.getSource()?.clear();
    if (!markerLocationUpdate) {
      return;
    }
    const marker = new Feature({
      geometry: new Point(fromLonLat([markerLocationUpdate.longitude, markerLocationUpdate.latitude]))
    });
    const rotationRadians = markerLocationUpdate.rotation * (Math.PI / 180);
    marker.setStyle(
      new Style({
        image: new Icon({
          anchor: [0.5, 0.5],
          src: 'assets/glider.png',
          rotateWithView: false,
          rotation: rotationRadians,
          scale: 0.15
        }),
      }),
    );
    this.flightPathMarkerVectorLayer.getSource()?.addFeature(marker);
  }

  private setupTimerForGliderPositionUpdates() {
    interval(this.settings.updateTimeout)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(() => {
        this.loadFlightsWithParams();
      });
  }

  private loadFlightsWithParams() {
    const extent = this.map.getView().calculateExtent(this.map.getSize());
    // Transform the extent to EPSG:4326
    const extentLonLat = transformExtent(extent, 'EPSG:3857', 'EPSG:4326');
    const [minLng, minLat, maxLng, maxLat] = extentLonLat;
    this.store.dispatch(loadFlights({
      maxLat,
      minLat,
      maxLng,
      minLng,
      selectedFlarmId: this.selectedFlight?.flarmId,
      clubGlidersOnly: this.settings.gliderFilterOnMap === GliderType.club ? true : false
    }))
  }

  private chaikinsAlgorithm(
    coords: Coordinate[],
    iterations: number = 3,
    factor: number = 0.25
  ): Coordinate[] {
    let result = coords;
    for (let i = 0; i < iterations; i++) {
      let smoothedCoords: Coordinate[] = [];
      for (let i = 0; i < result.length - 1; i++) {
        const p0 = result[i];
        const p1 = result[i + 1];

        const Q: Coordinate = [
          (1 - factor) * p0[0] + factor * p1[0],
          (1 - factor) * p0[1] + factor * p1[1],
        ];
        const R: Coordinate = [
          factor * p0[0] + (1 - factor) * p1[0],
          factor * p0[1] + (1 - factor) * p1[1],
        ];
        smoothedCoords.push(Q);
        smoothedCoords.push(R);
      }
      result = smoothedCoords;
    }
    // Add the original end points
    result.unshift(coords[0]);
    result.push(coords[coords.length - 1]);
    return result;
  }

  private onMarkerClicked(e: any) {
    const features = this.map.getFeaturesAtPixel(e.pixel, {hitTolerance: 4});
      if (!features || features.length < 1) {
        return;
      }
      const feature = features[0];
      const flarmId = feature.get('flarmId');
      if (flarmId) {
        this.selectGlider(flarmId);
      }
  }

  private initializeMap() {
    // Load the stored map state or use the default values
    const storedCenter = sessionStorage.getItem('mapCenter');
    const storedZoom = sessionStorage.getItem('mapZoom');
    const initialCenter = storedCenter
      ? JSON.parse(storedCenter)
      : coordinates.loxn;
    const initialZoom = storedZoom ? +storedZoom : 12;

    this.backgroundTileLayer = new TileLayer({
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
    this.flightPathMarkerVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    const mapView = new View({
      center: fromLonLat(initialCenter),
      zoom: initialZoom,
      minZoom: 6,
      enableRotation: false
    });
    // Always store current map center and zoom in session storage
    mapView.on('change', () => {
      const zoom = this.map.getView().getZoom();
      const center = this.map.getView().getCenter();
      let hasAnyValuesChanged = false;

      if (zoom !== undefined) {
        const storedZoom = sessionStorage.getItem('mapZoom');
        if (storedZoom === null || storedZoom !== zoom.toString()) {
          sessionStorage.setItem('mapZoom', zoom.toString());
          hasAnyValuesChanged = true;
        }
      }
      if (center) {
        const storedCenter = sessionStorage.getItem('mapCenter');
        const lonLatCenter = toLonLat(center);
        if (storedCenter === null || storedCenter !== JSON.stringify(lonLatCenter)) {
          sessionStorage.setItem('mapCenter', JSON.stringify(lonLatCenter));
          hasAnyValuesChanged = true;
        }
      }
      if (hasAnyValuesChanged) {
        this.loadFlightsWithParams();
      }
    });

    this.map = new OlMap({
      target: 'map',
      layers: [
        this.backgroundTileLayer,
        this.flightPathStrokeVectorLayer,
        this.flightPathVectorLayer,
        this.flightPathMarkerVectorLayer,
        this.glidersVectorLayer,
      ],
      view: mapView,
    });

    // change mouse cursor to "pointer" when hovering over marker
    this.map.on('pointermove', (e) => {
      const hit = this.map.hasFeatureAtPixel(e.pixel);
      this.map.getTargetElement().style.cursor = hit ? 'pointer' : '';
    });
    // Show information card and load flight path when marker is clicked
    this.map.on('click', (e) => {
      this.onMarkerClicked(e);
    });
  }
}
