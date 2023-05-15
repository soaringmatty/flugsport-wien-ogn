import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType } from 'chart.js';
import { flightPathDarkRed, groundHeightBackgroundBrown, groundHeightBrown } from 'src/ogn/services/marker-style.utils';
import 'chartjs-adapter-date-fns';
import {de} from 'date-fns/locale';
import { Subject, takeUntil } from 'rxjs';
import { Store } from '@ngrx/store';
import { State } from 'src/app/store';
import { HistoryEntry } from 'src/ogn/models/history-entry.model';
import { BaseChartDirective } from 'ng2-charts';

interface FlightData {
  timestamp: number;
  altitude: number;
  groundHeight: number;
  x: number;
}

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
      
    },
    interaction: {
      intersect: false,
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
    this.lineChartLabels.push(historyEntry.unixTimestamp);
    this.lineChartData[0].data.push(historyEntry.altitude);
    this.lineChartData[1].data.push(historyEntry.groundHeight);
    this.chart?.update();
  }

  private initalizeChart(): void {
    if (!this.flightHistory) {
      return;
    }
    this.lineChartLabels = this.flightHistory.map(x => x.unixTimestamp)
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