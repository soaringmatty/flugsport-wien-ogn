import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ChartDataset, ChartOptions, ChartType, Tooltip, TooltipPositionerFunction } from 'chart.js';
import 'chartjs-adapter-date-fns';
import 'chartjs-plugin-crosshair';
import {de} from 'date-fns/locale';
import { Subject, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { HistoryEntry } from 'src/ogn/models/history-entry.model';
import { BaseChartDirective } from 'ng2-charts';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { flightPathDarkRed, groundHeightBackgroundBrown } from 'src/ogn/services/glider-marker.service';
import { mobileLayoutBreakpoints } from 'src/ogn/constants/layouts';
import { FlightAnalysationService } from 'src/ogn/services/flight-analysation.service';
import { MapSettings } from 'src/ogn/models/settings.model';
import { MapBarogramSyncService } from 'src/ogn/services/map-barogram-sync.service';

declare module 'chart.js' {
  interface TooltipPositionerMap {
    center: TooltipPositionerFunction<ChartType>;
  }
};

@Component({
  selector: 'app-barogram',
  templateUrl: './barogram.component.html',
  styleUrls: ['./barogram.component.scss']
})
export class BarogramComponent implements OnInit, OnDestroy {
  @ViewChild(BaseChartDirective) chart?: BaseChartDirective;

  settings!: MapSettings;
  isMobilePortrait: boolean = false;
  flightHistory: HistoryEntry[] = [];
  lineChartData: ChartDataset[] = [];
  lineChartLabels: number[] = [];
  lineChartOptions: ChartOptions = this.setChartOptions();

  private readonly onDestroy$ = new Subject<void>();

  constructor(
    private store: Store<State>,
    private breakpointObserver: BreakpointObserver,
    private flightAnalysationService: FlightAnalysationService,
    private mapBarogramSyncService: MapBarogramSyncService
  ) {
    // Create custom tooltip position "center" -> currently not used
    Tooltip.positioners.center = function(elements, eventPosition) {
      return {
        x: eventPosition.x,
        y: this.chart.chartArea.height / 2,
        xAlign: 'center',
        yAlign: 'bottom',
      };
    };
  }

  ngOnInit() {
    this.breakpointObserver.observe(mobileLayoutBreakpoints).subscribe(result => {
      this.isMobilePortrait = result.matches;
      this.lineChartOptions = this.setChartOptions();
    });
    this.store
      .select((x) => x.app.settings)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(settings => {
        this.settings = settings;
      });
    this.store
      .select((x) => x.app.flightHistory)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(flightHistory => {
        if (this.settings.onlyShowLastFlight) {
          const filteredHistoryEntries = this.flightAnalysationService.getHistorySinceLastTakeoff(flightHistory);
          this.flightHistory = filteredHistoryEntries;
        }
        else {
          this.flightHistory = flightHistory;
        }
        this.initalizeChart();
      });
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  private updateLocationMarkerOnMap(timestamp: number) {
    const historyEntryIndex = this.flightHistory.findIndex(x => x.timestamp === timestamp);
    const historyEntry = this.flightHistory[historyEntryIndex]

    let direction = 0;
    if (this.flightHistory.length > 1 && historyEntryIndex > 0) {
        const previousHistoryEntry = this.flightHistory[historyEntryIndex - 1]
        direction = this.getDirection(
            [previousHistoryEntry.longitude, previousHistoryEntry.latitude],
            [historyEntry.longitude, historyEntry.latitude]
        );
    }
    else if (this.flightHistory.length > 1 && historyEntryIndex === 0) {
      const nextHistoryEntry = this.flightHistory[historyEntryIndex + 1]
        direction = this.getDirection(
            [historyEntry.longitude, historyEntry.latitude],
            [nextHistoryEntry.longitude, nextHistoryEntry.latitude]
        );
    }
    this.mapBarogramSyncService.updateLocationMarkerOnMap({
      longitude: historyEntry?.longitude,
      latitude: historyEntry?.latitude,
      rotation: direction
    })
  }

  private getDirection(coord1: [number, number], coord2: [number, number]): number {
    const lon1 = this.toRadians(coord1[0]);
    const lat1 = this.toRadians(coord1[1]);
    const lon2 = this.toRadians(coord2[0]);
    const lat2 = this.toRadians(coord2[1]);

    const dLon = lon2 - lon1;

    const y = Math.sin(dLon) * Math.cos(lat2);
    const x = Math.cos(lat1) * Math.sin(lat2) - Math.sin(lat1) * Math.cos(lat2) * Math.cos(dLon);

    let brng = this.toDegrees(Math.atan2(y, x));

    // Since atan2 returns values from -π to +π, we need to normalize the result by converting it to a compass bearing as measured in degrees clockwise from North.
    brng = (brng + 360) % 360;

    return brng;
  }

  private toRadians(degrees: number): number {
      return degrees * Math.PI / 180;
  }

  private toDegrees(radians: number): number {
      return radians * 180 / Math.PI;
  }

  private setChartOptions(): ChartOptions {
    return {
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      plugins: {
        title: {
          display: !this.isMobilePortrait,
          text: 'Höhendiagramm'
        },
        tooltip: {
          displayColors: false, // Hide the color boxes next to the labels
          position: 'average',
          callbacks: {
            label: (context) => {
              const value = context.raw as number;
              return `${context.dataset.label}: ${Math.round(value)} m`;
            },
            afterFooter: (context) => {
              const timestamp: number = context[0].parsed.x;
              this.updateLocationMarkerOnMap(timestamp);
            },
            afterBody: (context) => {
              const altitude = context[0].raw as number;
              const groundHeight = context[1].raw as number;
              const agl = altitude - groundHeight;

              return `AGL: ${Math.round(agl)} m`;
            },
          }
        },
        // crosshair: {
        //   line: {
        //     color: '#000000',  // change this as needed
        //     width: 1,
        //     dashPattern: [5, 5]  // make the line dashed (optional)
        //   },
        //   sync: {
        //     enabled: false  // do not trace the line at the same position for all charts
        //   }
        // },
      },
      interaction: {
        intersect: false,
        mode: 'index',
        axis: 'x'
      },
      scales: {
        x: {
          display: true,
          title: {
            display: !this.isMobilePortrait,
            text: 'Zeit'
          },
          type: 'time',
          time: {
            unit: 'minute',
            displayFormats: {
              minute: 'HH:mm'
            },
            tooltipFormat: 'HH:mm',
          },
          ticks: {
            source: 'auto',
            maxRotation: 0,
            minRotation: 0,
            autoSkip: true,
            maxTicksLimit: 7
          },
          adapters: {
            date: {
              locale: de
            }
          }
        },
        y: {
          display: true,
          title: {
            display: true,
            text: 'Höhe (MSL)'
          },
          beginAtZero: true,
        }
      },
    };
  }

  private initalizeChart(): void {
    if (!this.flightHistory) {
      return;
    }
    this.lineChartLabels = this.flightHistory.map(x => x.timestamp)
    this.lineChartData = [
      {
        data: this.flightHistory.map(x => x.altitude),
        label: 'Flughöhe',
        fill: false,
        borderWidth: 2,
        borderColor: flightPathDarkRed,
        tension: 0.4,
        pointStyle: false
      },
      {
        data: this.flightHistory.map(x => x.groundHeight),
        label: 'Boden',
        fill: true,
        borderWidth: 0,
        backgroundColor: groundHeightBackgroundBrown,
        tension: 0.4,
        pointStyle: false
      }
    ];
  }
}
