import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Flight } from 'src/ogn/models/flight.model';
import { Observable, Subject, Subscription, interval, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { loadFlightHistory, loadFlights, selectFlight } from 'src/app/store/app/app.actions';
import { MapSettings } from 'src/ogn/models/settings.model';
import { BarogramComponent } from '../barogram/barogram.component';
import { HistoryEntry } from 'src/ogn/models/history-entry.model';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { coordinates } from 'src/ogn/constants/coordinates';
import { BreakpointObserver } from '@angular/cdk/layout';
import { MapType } from 'src/ogn/models/map-type';
import { GliderMarkerProperties, GliderMarkerService } from 'src/ogn/services/glider-marker.service';
import { FlightAnalysationService } from 'src/ogn/services/flight-analysation.service';
import { GliderType } from 'src/ogn/models/glider-type';
import { mobileLayoutBreakpoints } from 'src/ogn/constants/layouts';
import { MapBarogramSyncService, MarkerLocationUpdate } from 'src/ogn/services/map-barogram-sync.service';
import OlMap from 'ol/Map';
import Style from 'ol/style/Style';
import Icon from 'ol/style/Icon';
import VectorSource from 'ol/source/Vector';
import TileSource from 'ol/source/Tile';
import OSM from 'ol/source/OSM';
import Stamen from 'ol/source/Stamen';
import Feature from 'ol/Feature';
import View from 'ol/View';
import { Coordinate } from 'ol/coordinate';
import { fromLonLat, toLonLat, transformExtent } from 'ol/proj';
import Point from 'ol/geom/Point';
import LineString from 'ol/geom/LineString';
import TileLayer from 'ol/layer/Tile';
import VectorLayer from 'ol/layer/Vector';
import { chaikinsAlgorithm } from 'src/ogn/utils/flight-path.utils';
import { MarkerColorScheme } from 'src/ogn/models/marker-color-scheme';
import { getRefreshTimeout } from 'src/ogn/services/settings.service';
import { GliderFilter } from 'src/ogn/models/glider-filter';

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss'],
})
export class MapComponent implements OnInit, OnDestroy {
  @ViewChild(BarogramComponent, { static: false }) barogram!: BarogramComponent;

  selectedFlight: Flight | undefined;
  showBarogram: boolean = false;
  isMobilePortrait: boolean = false;
  settings!: MapSettings;

  private flights: Flight[] = [];
  private markerDictionary: Map<string, GliderMarkerProperties> = new Map();
  private reloadInterval!: Subscription;
  private mapChangeTriggerTimeout: any;
  // Tracking related properties
  private isTracking: boolean = false;
  private trackingSubscription!: Subscription;
  private mapZoomBeforeActiveTracking: number | undefined;
  private mapCenterBeforeActiveTracking: Coordinate | undefined;
  // Map and Layers
  private map!: OlMap;
  private clubGlidersLayer!: VectorLayer<VectorSource>;
  private privateGlidersLayer!: VectorLayer<VectorSource>;
  private foreignGlidersLayer!: VectorLayer<VectorSource>;
  private flightPathStrokeLayer!: VectorLayer<VectorSource>;
  private flightPathLayer!: VectorLayer<VectorSource>;
  private flightPathMarkerLayer!: VectorLayer<VectorSource>;
  private mapTileLayer!: TileLayer<TileSource>

  private readonly onDestroy$ = new Subject<void>();

  constructor(
    private store: Store<State>,
    private route: ActivatedRoute,
    private breakpointObserver: BreakpointObserver,
    private gliderMarkerService: GliderMarkerService,
    private flightAnalysationService: FlightAnalysationService,
    private mapBarogramSyncService: MapBarogramSyncService
  ) { }

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
        const settingsAlreadyInitialized = this.settings ? true : false;
        this.settings = settings
        this.setMapTilesAccordingToSettings();
        if (settingsAlreadyInitialized) {
          // Reload data if settings are manually updated
          this.markerDictionary.clear();
          this.clubGlidersLayer.getSource()?.clear();
          this.privateGlidersLayer.getSource()?.clear();
          this.foreignGlidersLayer.getSource()?.clear();
          this.loadFlightsWithFilter();
          if (this.selectedFlight) {
            this.store.dispatch(loadFlightHistory({ flarmId: this.selectedFlight.flarmId }));
          }
        }
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
    this.route.paramMap
      .pipe(takeUntil(this.onDestroy$))
      .subscribe((params: ParamMap) => {
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
    }
    else {
      this.stopActiveTracking();
    }
    this.isTracking = newIsTracking;
  }

  toggleBarogram(newShowBarogram: boolean): void {
    this.showBarogram = newShowBarogram;
    // Remove location marker from map if barogram is hidden
    if (!newShowBarogram) {
      this.mapBarogramSyncService.updateLocationMarkerOnMap();
    }
  }

  // Removes focus from currently selected glider, clears flight path from map and closes info card and barogram
  unselectGlider(): void {
    const flightToUnselect = this.selectedFlight;
    if (!flightToUnselect) {
      return;
    }
    this.showBarogram = false;
    this.store.dispatch(selectFlight({ flight: null }))
    this.updateSingleMarkerOnMap(flightToUnselect)
    this.flightPathStrokeLayer.getSource()?.clear();
    this.flightPathLayer.getSource()?.clear();
    this.flightPathMarkerLayer.getSource()?.clear();
    this.stopActiveTracking();
  }

  // Puts a specific glider on the map in focus
  // -> Updates selected flight in store (which causes the info card to open), updates marker style to selected, loads flight path to show on map
  private selectGlider(flarmId: string): void {
    if (this.selectedFlight?.flarmId === flarmId) {
      return;
    }
    const previousSelectedFlight = this.selectedFlight;
    const flight = this.flights.find((x) => x.flarmId === flarmId);
    if (!flight) {
      console.warn(`Failed to select marker with flarmId ${flarmId} since it does not exist in list of flights`);
      return;
    }
    this.store.dispatch(selectFlight({ flight }));
    if (previousSelectedFlight) {
      // Update previous selected marker style to unselected
      this.updateSingleMarkerOnMap(previousSelectedFlight);
    }
    this.updateSingleMarkerOnMap(flight);
    this.store.dispatch(loadFlightHistory({ flarmId }));
  }

  // Initially load and draw glider positions on map
  private initiallyLoadData(): void {
    // Wait until font for marker text is loaded -> else the markers are generated before the font is loaded
    document.fonts.load('bold 26px Roboto').then(() => {
      this.loadFlightsWithFilter();
      this.setupTimerForGliderPositionUpdates();
    });
  }

  // Start to live track the selected gliders position (always keep map centered on glider)
  private startActiveTracking(): void {
    if (!this.selectedFlight) {
      console.warn('Unable to start active tracking. No glider selected');
      return;
    }
    this.isTracking = true;
    // Save map state to restore it after active tracking is stopped
    this.mapZoomBeforeActiveTracking = this.map.getView().getZoom();
    this.mapCenterBeforeActiveTracking = this.map.getView().getCenter();
    const flight = this.flights.find(x => x.flarmId === this.selectedFlight?.flarmId);
    if (!flight) {
      console.warn(`Unable to start active tracking. Selected glider with flarmId ${this.selectedFlight?.flarmId} does not exist in list of flights`);
      return;
    }
    const coordinate = fromLonLat([flight.longitude, flight.latitude]);
    this.map.getView().setCenter(coordinate);
    this.map.getView().setZoom(this.isMobilePortrait ? 13 : 15);

    // Subscribe to position updates
    this.trackingSubscription = this.store
      .select((x) => x.app.flights)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(flights => this.updateTrackingViewport(flights));
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

  // Sets map center to the current location of the selected glider
  private updateTrackingViewport(updatedFlights: Flight[]) {
    const updatedFlight = updatedFlights.find((x) => x.flarmId === this.selectedFlight?.flarmId);
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
  }

  // Adds, updates or removes markers on map according to loaded flights
  private updateGliderPositionsOnMap(flights: Flight[]) {
    flights.forEach(flight => {
      this.updateSingleMarkerOnMap(flight)
    });
    this.removeObsoleteGliderMarkers(flights);
  }

  // Remove markers of flights that no longer exist
  private removeObsoleteGliderMarkers(flights: Flight[]) {
    const gliderMarkersLayer = [this.clubGlidersLayer, this.privateGlidersLayer, this.foreignGlidersLayer];
    gliderMarkersLayer.forEach(layer => this.removeObsoleteGliderMarkersFromLayer(layer, flights))
  }

  private removeObsoleteGliderMarkersFromLayer(layer: VectorLayer<VectorSource>, flights: Flight[]) {
    layer.getSource()?.getFeatures()
      .forEach((feature) => {
        const flarmId = feature.getId();
        const flightStillExists = flights.find((flight) => flight.flarmId === flarmId)
        if (!flightStillExists) {
          layer.getSource()?.removeFeature(feature);
        }
      }
    );
  }

  // Adds, updates or removes single marker on map depending on the given flight
  private async updateSingleMarkerOnMap(flight: Flight) {
    if (!flight || !flight.longitude || !flight.latitude) {
      return;
    }
    const layer = this.getLayerByGliderType(flight.type);
    const existingFeature = layer.getSource()?.getFeatureById(flight.flarmId);
    // If marker already exists and just got selected or unseleted -> update it's position and style
    // If marker already exists and has no selection event -> just update it's position
    if (existingFeature) {
      existingFeature.setGeometry(
        new Point(fromLonLat([flight.longitude, flight.latitude]))
      );
      const shouldUpdateStyle = this.doesMarkerNeedStyleUpdate(flight);
      if (shouldUpdateStyle) {
        const isSelected = this.selectedFlight?.flarmId === flight.flarmId;
        const iconStyle = await this.gliderMarkerService.getGliderMarkerStyle(flight, this.settings, isSelected);
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
      const iconStyle = await this.gliderMarkerService.getGliderMarkerStyle(flight, this.settings, isSelected);
      gliderMarkerFeature.setStyle(iconStyle);
      layer.getSource()?.addFeature(gliderMarkerFeature);
    }
    this.updateMarkerInDictionary(flight);
  }

  // Returns value that indicates whether or not a specific marker needs an style update depending on its selection status or last update timestamp
  private doesMarkerNeedStyleUpdate(flight: Flight): boolean {
    const lastProperties = this.markerDictionary.get(flight.flarmId);
    if (!lastProperties) {
      return true;
    }
    const isFlightSelected = this.selectedFlight?.flarmId === flight.flarmId;
    if (lastProperties.isSelected !== isFlightSelected) {
      return true;
    }
    const newOpacity = this.gliderMarkerService.getMarkerOpacityByLastUpdateTimestamp(flight.timestamp);
    if (lastProperties.opacity !== newOpacity) {
      return true;
    }
    const newAltitudeLayer = Math.floor(flight.heightMSL / 250)
    if (this.settings.markerColorScheme === MarkerColorScheme.altitude && lastProperties.altitudeLayer !== newAltitudeLayer) {
      return true;
    }
    return false;
  }

  // Stores marker style affecting information about a specific flight in a dictionary
  private updateMarkerInDictionary(flight: Flight): void {
    const lastProperties = this.markerDictionary.get(flight.flarmId);
    const opacity = this.gliderMarkerService.getMarkerOpacityByLastUpdateTimestamp(flight.timestamp);
    let newProperties: GliderMarkerProperties = {
      isSelected: this.selectedFlight?.flarmId === flight.flarmId,
      opacity,
      altitudeLayer: Math.floor(flight.heightMSL / 250)
    }
    if (lastProperties) {
      newProperties = {
        ...lastProperties,
        isSelected: this.selectedFlight?.flarmId === flight.flarmId,
        opacity,
        altitudeLayer: Math.floor(flight.heightMSL / 250)
      }
    }
    this.markerDictionary.set(flight.flarmId, newProperties);
  }

  // Draws flight path from a list of history entries on the map
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
      const smoothedCoords: Coordinate[] = chaikinsAlgorithm(coordinates);
      if (!smoothedCoords?.length) {
        return;
      }
      geometry = new LineString(smoothedCoords);
    }

    const outerLineFeature = new Feature(geometry);
    const innerLineFeature = new Feature(geometry);
    outerLineFeature.setStyle(this.gliderMarkerService.flightPathStrokeStyle);
    innerLineFeature.setStyle(this.gliderMarkerService.flightPathStyle);

    this.flightPathStrokeLayer.getSource()?.clear();
    this.flightPathStrokeLayer.getSource()?.addFeature(outerLineFeature);
    this.flightPathLayer.getSource()?.clear();
    this.flightPathLayer.getSource()?.addFeature(innerLineFeature);
  }

  // Draws a marker on the corresponding location on the flight path when a timestamp is selected in the barogram
  private drawMarkerOnFlightPath(markerLocationUpdate: MarkerLocationUpdate) {
    this.flightPathMarkerLayer.getSource()?.clear();
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
    this.flightPathMarkerLayer.getSource()?.addFeature(marker);
  }

  // Setup timer that reloads flights on the map every few seconds
  private setupTimerForGliderPositionUpdates() {
    this.reloadInterval?.unsubscribe();
    const refreshTimeout = getRefreshTimeout(this.settings.reduceDataUsage);
    this.reloadInterval = interval(refreshTimeout)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(() => {
        this.loadFlightsWithFilter();
      });
  }

  // Loads flights for the current viewport with selected filtesr from settings
  private loadFlightsWithFilter() {
    const extent = this.map.getView().calculateExtent(this.map.getSize());
    const extentLonLat = transformExtent(extent, 'EPSG:3857', 'EPSG:4326');
    const [minLng, minLat, maxLng, maxLat] = extentLonLat;
    this.store.dispatch(loadFlights({
      maxLat,
      minLat,
      maxLng,
      minLng,
      selectedFlarmId: this.selectedFlight?.flarmId,
      glidersOnly: this.settings.gliderFilterOnMap === GliderFilter.allAirplanes ? false : true,
      clubGlidersOnly: this.settings.gliderFilterOnMap === GliderFilter.club ? true : false
    }))
  }

  // Selects specific glider marker bases on a map click event
  private onMarkerClicked(e: any) {
    const features = this.map.getFeaturesAtPixel(e.pixel, { hitTolerance: 4 });
    if (!features || features.length < 1) {
      return;
    }
    const feature = features[0];
    const flarmId = feature.get('flarmId');
    if (flarmId) {
      this.selectGlider(flarmId);
    }
  }

  // Updates map tiles according to settings
  private setMapTilesAccordingToSettings(): void {
    let source = null;
    switch (this.settings.mapType) {
      case MapType.stamen:
        source = new Stamen({ layer: 'terrain' });
        break;
      case MapType.osm:
      default:
        source = new OSM()
        break;
    }
    this.mapTileLayer.setSource(source);
  }

  // Get marker layer depending on glider type
  private getLayerByGliderType(gliderType: GliderType): VectorLayer<VectorSource> {
    switch (gliderType) {
      case GliderType.club:
        return this.clubGlidersLayer;
      case GliderType.private:
        return this.privateGlidersLayer;
      default:
        return this.foreignGlidersLayer;
    }
  }

  // Store current map center and zoom in session storage and reload flights for new viewport
  private handleMapViewChange() {
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
      this.setupTimerForGliderPositionUpdates();
      this.loadFlightsWithFilter();
    }
  }

  // Initialize map (Layers, Events, Position, Zoom)
  private initializeMap() {
    // Load the stored map state or use the default values
    const storedCenter = sessionStorage.getItem('mapCenter');
    const storedZoom = sessionStorage.getItem('mapZoom');
    const initialCenter = storedCenter
      ? JSON.parse(storedCenter)
      : coordinates.loxn;
    const initialZoom = storedZoom ? +storedZoom : 12;

    this.mapTileLayer = new TileLayer({source: new OSM()});
    this.clubGlidersLayer = new VectorLayer({source: new VectorSource()});
    this.privateGlidersLayer = new VectorLayer({source: new VectorSource()});
    this.foreignGlidersLayer = new VectorLayer({source: new VectorSource()});
    this.flightPathStrokeLayer = new VectorLayer({source: new VectorSource()});
    this.flightPathLayer = new VectorLayer({source: new VectorSource()});
    this.flightPathMarkerLayer = new VectorLayer({source: new VectorSource()});

    const mapView = new View({
      center: fromLonLat(initialCenter),
      zoom: initialZoom,
      minZoom: 6,
      enableRotation: false
    });

  // Store current map center and zoom in session storage and reload flights for new viewport
    mapView.on('change', () => {
      if (!this.settings.reduceDataUsage) {
        this.handleMapViewChange();
        return;
      }
      // Delay reloading data when reduceDataUsage is active in settings
      if (this.mapChangeTriggerTimeout) {
        clearTimeout(this.mapChangeTriggerTimeout);
      }
      this.mapChangeTriggerTimeout = setTimeout(() => this.handleMapViewChange(), 600); // 600ms delay
    });

    this.map = new OlMap({
      target: 'map',
      layers: [
        this.mapTileLayer,
        this.flightPathStrokeLayer,
        this.flightPathLayer,
        this.flightPathMarkerLayer,
        this.foreignGlidersLayer,
        this.privateGlidersLayer,
        this.clubGlidersLayer,
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
