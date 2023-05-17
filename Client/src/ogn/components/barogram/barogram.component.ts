import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType, Color, Colors, Tooltip, TooltipPositionerFunction } from 'chart.js';
import { flightPathDarkRed, groundHeightBackgroundBrown, groundHeightBrown } from 'src/ogn/services/marker-style.utils';
import 'chartjs-adapter-date-fns';
import 'chartjs-plugin-crosshair';
import {de} from 'date-fns/locale';
import { Subject, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { HistoryEntry } from 'src/ogn/models/history-entry.model';
import { BaseChartDirective } from 'ng2-charts';
import { CrosshairOptions } from 'chartjs-plugin-crosshair';

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

  flightHistory: HistoryEntry[] = [];
  lineChartData: ChartDataset[] = [];
  lineChartLabels: number[] = [];
  lineChartOptions: ChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    animation: false,
    plugins: {
      title: {
        display: true,
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
          display: true,
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

  private readonly onDestroy$ = new Subject<void>();

  constructor(private store: Store<State>) {
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
    this.store
      .select((x) => x.app.flightHistory)
      .pipe(takeUntil(this.onDestroy$))
      .subscribe(flightHistory => {
        this.flightHistory = flightHistory;
        this.initalizeChart();
      });
  }

  ngOnDestroy(): void {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  addValue(historyEntry: HistoryEntry): void {
    if (this.lineChartData?.length < 2) {
      console.warn('Unable to insert value to barogram. Chart is not initialized')
      return;
    }
    this.lineChartLabels.push(historyEntry.timestamp);
    this.lineChartData[0].data.push(historyEntry.altitude);
    this.lineChartData[1].data.push(historyEntry.groundHeight);
    this.chart?.update();
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
        borderColor: flightPathDarkRed,
        tension: 0.4,
        pointStyle: false
      },
      {
        data: this.flightHistory.map(x => x.groundHeight),
        label: 'Boden',
        fill: true,
        borderColor: groundHeightBrown,
        backgroundColor: groundHeightBackgroundBrown,
        tension: 0.4,
        pointStyle: false
      }
    ];
  }
}
