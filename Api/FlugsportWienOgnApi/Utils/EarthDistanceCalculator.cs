using FlugsportWienOgnApi.Models.Core;

namespace FlugsportWienOgnApi.Utils;

public static class EarthDistanceCalculator
{
    /// <summary>
    /// Returns distance between two coordinates in meters
    /// </summary>
    /// <param name="lat1"></param>
    /// <param name="lon1"></param>
    /// <param name="lat2"></param>
    /// <param name="lon2"></param>
    /// <returns></returns>
    public static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371e3; // Earth's radius in meters
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var deltaPhi = (lat2 - lat1) * Math.PI / 180;
        var deltaLambda = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return r * c; // in meters
    }

    public static double DistancePointToSegment(Coordinate p, Coordinate a, Coordinate b)
    {
        // Konvertiere Längen- und Breitengrad zu kartesischem Raum (einfache Projektion)
        // Achtung: für größere Distanzen wäre dies ungenau – hier reicht's.
        double x = LonToX(p.Longitude);
        double y = LatToY(p.Latitude);
        double x1 = LonToX(a.Longitude);
        double y1 = LatToY(a.Latitude);
        double x2 = LonToX(b.Longitude);
        double y2 = LatToY(b.Latitude);

        double dx = x2 - x1;
        double dy = y2 - y1;

        if (dx == 0 && dy == 0)
            return CalculateHaversineDistance(p.Latitude, p.Longitude, a.Latitude, a.Longitude); // Segment ist nur ein Punkt

        double t = ((x - x1) * dx + (y - y1) * dy) / (dx * dx + dy * dy);
        t = Math.Max(0, Math.Min(1, t));

        double projX = x1 + t * dx;
        double projY = y1 + t * dy;

        double latProj = YToLat(projY);
        double lonProj = XToLon(projX);

        return CalculateHaversineDistance(p.Latitude, p.Longitude, latProj, lonProj);
    }

    private const double EarthRadius = 6371000.0;
    private static double LatToY(double lat) => EarthRadius * Math.Log(Math.Tan(Math.PI / 4 + (lat * Math.PI / 180) / 2));
    private static double LonToX(double lon) => EarthRadius * lon * Math.PI / 180;
    private static double YToLat(double y) => (2 * Math.Atan(Math.Exp(y / EarthRadius)) - Math.PI / 2) * 180 / Math.PI;
    private static double XToLon(double x) => x * 180 / (Math.PI * EarthRadius);
}
