using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FlugsportWienOgnApi.Utils;

public class AustriaGeoCalculator
{
    private readonly GeometryFactory _geometryFacotry;
    private readonly Polygon _austriaPolygon = new Polygon(new LinearRing(_austrianBoundary.ToArray()));

    public AustriaGeoCalculator()
    {
        _geometryFacotry = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var linearRing = new LinearRing(_austrianBoundary.ToArray());
        _austriaPolygon = new Polygon(linearRing);
    }

    public bool IsPointInAustria(double longitude, double latitude)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        Point point = geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        return _austriaPolygon.Contains(point);
    }

    private static readonly List<Coordinate> _austrianBoundary =
    [
        new Coordinate(15.007324, 49.023461),
        new Coordinate(14.650269, 48.618385),
        new Coordinate(13.837280, 48.777913),
        new Coordinate(13.447266, 48.578424),
        new Coordinate(12.733154, 48.100095),
        new Coordinate(13.024292, 47.480088),
        new Coordinate(12.782593, 47.680183),
        new Coordinate(12.255249, 47.746711),
        new Coordinate(11.046753, 47.420654),
        new Coordinate(10.898438, 47.543164),
        new Coordinate(10.426025, 47.598755),
        new Coordinate(10.409546, 47.390912),
        new Coordinate(10.200806, 47.275502),
        new Coordinate(10.244751, 47.376035),
        new Coordinate(9.783325, 47.620975),
        new Coordinate(9.541626, 47.535747),
        new Coordinate(9.602051, 47.058896),
        new Coordinate(10.123901, 46.815099),
        new Coordinate(10.382080, 46.984000),
        new Coordinate(10.464478, 46.818858),
        new Coordinate(11.030273, 46.754917),
        new Coordinate(11.162109, 46.935261),
        new Coordinate(12.095947, 46.984000),
        new Coordinate(12.381592, 46.679594),
        new Coordinate(14.567871, 46.373464),
        new Coordinate(15.001831, 46.611715),
        new Coordinate(16.029053, 46.638122),
        new Coordinate(16.023560, 46.833892),
        new Coordinate(16.528931, 47.006480),
        new Coordinate(16.479492, 47.398349),
        new Coordinate(17.089233, 47.706065),
        new Coordinate(17.166138, 48.004625),
        new Coordinate(16.864014, 48.389090),
        new Coordinate(16.968384, 48.531157),
        new Coordinate(16.913452, 48.723585),
        new Coordinate(16.463013, 48.810481),
        new Coordinate(16.380615, 48.738078),
        new Coordinate(15.276489, 49.009051),
        new Coordinate(15.007324, 49.023461)
    ];
}
