import { Component, OnInit } from '@angular/core';
import { Chart, ChartDataset, ChartOptions, ChartType } from 'chart.js';
import { flightPathDarkRed, groundHeightBackgroundBrown, groundHeightBrown } from 'src/ogn/services/marker-style.utils';
import 'chartjs-adapter-date-fns';
import {de} from 'date-fns/locale';

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
export class BarogramComponent implements OnInit {
  flightDataRaw: number[][] = [
    [1684078857000,5.23,50.01293,10.55017,410.87,219],
    [1684078862000,3.62,50.01227,10.55323,430.99,219],
    [1684078869000,4.63,50.01128,10.55755,464.82,220],
    [1684078881000,3.02,50.00837,10.56393,512.06,220],
    [1684078886000,2.01,50.00617,10.56525,519.99,217],
    [1684078897000,5.23,50.01293,10.55017,410.87,219],
    [1684078899000,3.62,50.01227,10.55323,430.99,219],
    [1684078910000,4.63,50.01128,10.55755,464.82,220],
    [1684078915000,3.02,50.00837,10.56393,512.06,220],
    [1684078935000,2.01,50.00617,10.56525,519.99,217],
    [1684078940000,4.63,50.01128,10.55755,464.82,220],
    [1684078955000,3.02,50.00837,10.56393,512.06,220],
    [1684078965000,2.01,50.00617,10.56525,519.99,217],
    [1684079057000,5.23,50.01293,10.55017,410.87,219],
    [1684079062000,3.62,50.01227,10.55323,430.99,219],
    [1684079069000,4.63,50.01128,10.55755,464.82,220],
    [1684079081000,3.02,50.00837,10.56393,512.06,220],
    [1684079086000,2.01,50.00617,10.56525,519.99,217],
    [1684079097000,5.23,50.01293,10.55017,410.87,219],
    [1684079099000,3.62,50.01227,10.55323,430.99,219],
    [1684079110000,4.63,50.01128,10.55755,464.82,220],
    [1684079115000,3.02,50.00837,10.56393,512.06,220],
    [1684079135000,2.01,50.00617,10.56525,519.99,217],
    [1684079140000,4.63,50.01128,10.55755,464.82,220],
    [1684079155000,3.02,50.00837,10.56393,512.06,220],
    [1684079165000,2.01,50.00617,10.56525,519.99,217],
  ];
  flightData: FlightData[] = this.flightDataRaw.map(input => {
    return {
      timestamp: input[0],
      altitude: input[4],
      groundHeight: input[5],
      x:input[0]
    }
  });

  lineChartData: ChartDataset[] = [
    { 
      data: this.flightData.map(d => d.altitude), 
      label: 'Flughöhe', 
      fill: false, 
      borderColor: flightPathDarkRed,
      tension: 0.4
    },
    { 
      data: this.flightData.map(d => d.groundHeight), 
      label: 'Boden', 
      fill: true, 
      borderColor: groundHeightBrown,
      backgroundColor: groundHeightBackgroundBrown,
      tension: 0.4,
      pointStyle: 'false'
    }
  ];

  lineChartType: ChartType = 'line';
  lineChartLabels = this.flightData.map(x => x.timestamp);

  lineChartOptions: ChartOptions = {
    responsive: true,
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
        //beginAtZero: true,
      }
    },
  };
  

  constructor() { 
  }

  ngOnInit() {
  }
}