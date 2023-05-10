import { Component, OnDestroy, OnInit } from '@angular/core';
import { Flight } from 'src/ogn/models/flight.model';
import { OSM, Vector as VectorSource } from 'ol/source';
import { fromLonLat } from 'ol/proj';
import { Point } from 'ol/geom';
import { Vector as VectorLayer, Tile as TileLayer } from 'ol/layer';
import { Fill, Icon, Stroke, Style, Text } from 'ol/style';
import Polyline from 'ol/format/Polyline';
import { Map, View, Feature } from 'ol';
import { Subject, interval, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { loadFlightPath, loadFlights } from 'src/app/store/app/app.actions';
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

  constructor(private store: Store<State>) {}

  ngOnInit(): void {
    this.initializeMap();
    // Redraw markers on map on every update
    this.store.select(x => x.app.flights).pipe(
      takeUntil(this.onDestroy$)
    ).subscribe(flights => {
      this.updateGliderPositionsOnMap(flights);
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

  private updateGliderPositionsOnMap(flights: Flight[]) {
    this.map.removeLayer(this.glidersVectorLayer)
    this.glidersVectorLayer = new VectorLayer({
      source: new VectorSource(),
    });
    this.drawGliderMarkers(flights);
    this.map.addLayer(this.glidersVectorLayer);
  }

  // private drawGliderMarkers(flights: Flight[]) {
  //   flights.forEach(flight => {
  //     if (!flight.longitude || !flight.latitude) {
  //       return
  //     }
  //     const gliderMarkersFeature = new Feature({
  //       geometry: new Point(fromLonLat([flight.longitude, flight.latitude])),
  //       flarmId: flight.flarmId,
  //     });
  //     const iconStyle = new Style({
  //       image: new Icon({
  //         anchor: [0.5, 1],
  //         anchorXUnits: 'fraction',
  //         anchorYUnits: 'fraction',
  //         src: 'assets/marker_yellow.png',
  //         scale: 0.35,
  //       }),
  //       text: new Text({
  //         text: flight.displayName,
  //         font: '11px Roboto',
  //         fill: new Fill({
  //           color: 'black',
  //         }),
  //         offsetY: -17
  //       }),
  //     })
  //     gliderMarkersFeature.setStyle(iconStyle);
  //     this.glidersVectorLayer.getSource()?.addFeature(gliderMarkersFeature);
  //   });
  // }

  private drawGliderMarkers(flights: Flight[]) {
    flights.forEach(flight => {
      if (!flight.longitude || !flight.latitude) {
        return;
      }
  
      // Try to find an existing feature for this flight
      const existingFeature = this.glidersVectorLayer.getSource()?.getFeatureById(flight.flarmId);
      if (existingFeature) {
        // If the feature already exists, just update its geometry
        existingFeature.setGeometry(new Point(fromLonLat([flight.longitude, flight.latitude])));
      } else {
        // If the feature does not exist, create it
        const gliderMarkersFeature = new Feature({
          geometry: new Point(fromLonLat([flight.longitude, flight.latitude])),
          flarmId: flight.flarmId
        });
        gliderMarkersFeature.setId(flight.flarmId);  // Set the feature id to the flight flarmId
  
        // Set the style for the feature
        const iconStyle = new Style({
          image: new Icon({
            anchor: [0.5, 1],
            anchorXUnits: 'fraction',
            anchorYUnits: 'fraction',
            src: 'assets/marker_yellow.png',
            scale: 0.35,
          }),
          text: new Text({
            text: flight.displayName,
            font: '11px Roboto',
            fill: new Fill({
              color: 'black',
            }),
            offsetY: -17
          }),
        });
        gliderMarkersFeature.setStyle(iconStyle);
  
        // Add the feature to the layer's source
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
    // Dispatch store action when marker is clicked
    this.map.on('singleclick', (e) => {
      this.map.forEachFeatureAtPixel(e.pixel, (feature) => {
        // Assuming that the flight ID is stored as a property in the feature
        const flarmId = feature.get('flarmId');
        if (flarmId) {
          this.store.dispatch(loadFlightPath({flarmId}));
        }
      });
    });
  }
}
