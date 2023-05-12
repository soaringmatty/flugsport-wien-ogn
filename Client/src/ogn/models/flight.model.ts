export interface Flight {
  flarmId: string;
  displayName: string;
  registration: string;
  type: number;
  model: string;
  latitude: number;
  longitude: number;
  heightMSL: number;
  heightAGL: number;
  timestamp: number;
  speed: number;
  vario: number;
  varioAverage: number;
  receiver: string;
  receiverPosition: any;
  pilot: string;
  owner: string;
}
