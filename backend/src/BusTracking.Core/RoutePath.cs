namespace BusTracking.Core;

/// <summary>
/// A position on a route and the heading of the segment it lies on.
/// </summary>
public readonly record struct RoutePosition(GeoPoint Point, double HeadingDegrees);

/// <summary>
/// The line a bus follows, with the distance travelled precomputed at every vertex.
/// </summary>
public sealed class RoutePath
{
    private readonly GeoPoint[] _points;
    private readonly double[] _cumulativeMeters;

    public RoutePath(IReadOnlyList<GeoPoint> points)
    {
        if (points.Count < 2)
        {
            throw new ArgumentException("A route path needs at least two points.", nameof(points));
        }

        _points = points.ToArray();
        _cumulativeMeters = new double[_points.Length];
        for (var i = 1; i < _points.Length; i++)
        {
            _cumulativeMeters[i] = _cumulativeMeters[i - 1] + GeoMath.DistanceMeters(_points[i - 1], _points[i]);
        }

        TotalLengthMeters = _cumulativeMeters[^1];
    }

    public IReadOnlyList<GeoPoint> Points => _points;

    public double TotalLengthMeters { get; }

    /// <summary>
    /// The point reached after travelling <paramref name="distanceMeters"/> from the start.
    /// Distances outside the route are clamped to its first or last point.
    /// </summary>
    public RoutePosition PositionAt(double distanceMeters)
    {
        var distance = Math.Clamp(distanceMeters, 0, TotalLengthMeters);

        var index = Array.BinarySearch(_cumulativeMeters, distance);
        var segment = index >= 0 ? index : ~index - 1;
        segment = Math.Clamp(segment, 0, _points.Length - 2);

        var start = _points[segment];
        var end = _points[segment + 1];
        var segmentLength = _cumulativeMeters[segment + 1] - _cumulativeMeters[segment];
        var fraction = segmentLength > 0 ? (distance - _cumulativeMeters[segment]) / segmentLength : 0;

        // Linear interpolation is accurate enough for segments of a few hundred meters.
        var point = new GeoPoint(
            start.Latitude + ((end.Latitude - start.Latitude) * fraction),
            start.Longitude + ((end.Longitude - start.Longitude) * fraction));

        return new RoutePosition(point, GeoMath.BearingDegrees(start, end));
    }

    /// <summary>
    /// How far along the route, in meters from its start, the point of the line closest
    /// to <paramref name="point"/> lies. Points beyond either end clamp to 0 or the total length.
    /// </summary>
    /// <remarks>
    /// Each segment is projected on a local flat plane around its start, which is accurate for
    /// segments of a few hundred meters. A route that passes twice near the same place resolves
    /// to whichever segment is closest.
    /// </remarks>
    public double DistanceAlong(GeoPoint point) => Project(point).DistanceAlong;

    /// <summary>
    /// How far, in meters, <paramref name="point"/> is from the closest point of the line.
    /// Zero when it lies on the line; beyond either end it is the distance to that end.
    /// </summary>
    public double DistanceFromLine(GeoPoint point) => Project(point).DistanceFromLine;

    // Projects the point onto every segment and keeps the closest one.
    private (double DistanceAlong, double DistanceFromLine) Project(GeoPoint point)
    {
        var bestDistanceAlong = 0.0;
        var bestOffsetSquared = double.MaxValue;

        for (var segment = 0; segment < _points.Length - 1; segment++)
        {
            var start = _points[segment];
            var (endX, endY) = ToLocalMeters(start, _points[segment + 1]);
            var (pointX, pointY) = ToLocalMeters(start, point);

            var lengthSquared = (endX * endX) + (endY * endY);
            var fraction = lengthSquared > 0
                ? Math.Clamp(((pointX * endX) + (pointY * endY)) / lengthSquared, 0, 1)
                : 0;

            var offsetX = pointX - (fraction * endX);
            var offsetY = pointY - (fraction * endY);
            var offsetSquared = (offsetX * offsetX) + (offsetY * offsetY);

            if (offsetSquared < bestOffsetSquared)
            {
                bestOffsetSquared = offsetSquared;
                var segmentLength = _cumulativeMeters[segment + 1] - _cumulativeMeters[segment];
                bestDistanceAlong = _cumulativeMeters[segment] + (fraction * segmentLength);
            }
        }

        return (bestDistanceAlong, Math.Sqrt(bestOffsetSquared));
    }

    // Equirectangular projection: east and north offsets in meters from origin.
    private static (double X, double Y) ToLocalMeters(GeoPoint origin, GeoPoint point)
    {
        const double metersPerDegree = Math.PI * GeoMath.EarthRadiusMeters / 180;
        var x = (point.Longitude - origin.Longitude) * metersPerDegree * Math.Cos(origin.Latitude * Math.PI / 180);
        var y = (point.Latitude - origin.Latitude) * metersPerDegree;
        return (x, y);
    }
}
