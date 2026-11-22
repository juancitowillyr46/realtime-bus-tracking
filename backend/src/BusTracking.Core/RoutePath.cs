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
}
