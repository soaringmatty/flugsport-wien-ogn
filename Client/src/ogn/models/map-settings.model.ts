import { GliderType } from "./glider-type";
import { MapType } from "./map-type";
import { MarkerColorScheme } from "./marker-color-scheme";

export interface MapSettings {
    version: string;
    gliderFilterOnMap: GliderType;
    hideGlidersOnGround: boolean;
    showGlidersOnly: boolean;
    mapType: MapType
    useFlightPathSmoothing: boolean;
    onlyShowLastFlight: boolean;
    gliderFilterInLists: GliderType;
    showChangelogForNewVersion: boolean;
    markerColorScheme: MarkerColorScheme;
    updateTimeout: number;
    useUtcTimeInDepartureList: boolean;
}
