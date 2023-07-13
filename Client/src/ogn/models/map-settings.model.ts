import { GliderType } from "./glider-type";
import { MapType } from "./map-type";
import { MarkerColorScheme } from "./marker-color-scheme";

export interface MapSettings {
    version: string;
    gliderFilterOnMap: GliderType;
    hideGlidersOnGround: boolean;
    mapType: MapType
    useFlightPathSmoothing: boolean;
    gliderFilterInLists: GliderType;
    useExperimentalFeatures: boolean;
    useLowDataTransfer: boolean;
    showChangelogForNewVersion: boolean;
    markerColorScheme: MarkerColorScheme;
}
