export interface DepartureListItem {
  flarmId: string;
  registration: string;
  registrationShort: string;
  model: string;
  takeOffTimestamp?: number;
  landingTimestamp?: number;
}
