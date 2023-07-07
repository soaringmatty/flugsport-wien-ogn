import { GliderType } from "./glider-type";
import { MapType } from "./map-type";

export interface MapSettings {
    gliderFilterOnMap: GliderType;
    hideGlidersOnGround: boolean;
    mapType: MapType
    useFlightPathSmoothing: boolean;
    gliderFilterInLists: GliderType;
}
