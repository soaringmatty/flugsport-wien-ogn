import { GliderStatus } from "./glider-status";

export interface GliderListItem {
  owner: string;
  registration: string;
  registrationShort: string;
  model: string;
  status: GliderStatus;
  pilot: string;
  distanceFromHome: number;
  altitude: number;
  takeOffTimestamp: number;
}
