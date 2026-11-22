namespace BusTracking.Core.Routes;

public sealed record RouteStop(string StopId, string Name, int Order, GeoPoint Location);

/// <summary>
/// A route drawn in GeoJSON: its line and its stops, sorted by <see cref="RouteStop.Order"/>.
/// </summary>
public sealed record RouteDefinition(string RouteId, string Name, RoutePath Path, IReadOnlyList<RouteStop> Stops)
{
    /// <summary>
    /// A bus farther than this from the line is off the route: it has no next stop. GPS readings
    /// in a city are off by a few tens of meters, so the margin is generous.
    /// </summary>
    public const double OffRouteThresholdMeters = 150;

    public bool IsOffRoute(GeoPoint point) => Path.DistanceFromLine(point) > OffRouteThresholdMeters;

    /// <summary>
    /// The first stop at or ahead of <paramref name="point"/> along the route. A bus past the
    /// last stop is heading back to the first one, because buses start the route again.
    /// Returns <c>null</c> when the route has no stops or the point is off the route.
    /// </summary>
    public RouteStop? NextStop(GeoPoint point)
    {
        if (IsOffRoute(point))
        {
            return null;
        }

        var busDistance = Path.DistanceAlong(point);
        return Stops.FirstOrDefault(stop => Path.DistanceAlong(stop.Location) >= busDistance)
            ?? Stops.FirstOrDefault();
    }
}
