import { environment } from "./environment";

export const api = {
    getFlights: environment.api + 'flights',
    getFlightPath: environment.api + 'flights/{id}/path',
    getFlightHistory: environment.api + 'flights/{id}/history'
}
