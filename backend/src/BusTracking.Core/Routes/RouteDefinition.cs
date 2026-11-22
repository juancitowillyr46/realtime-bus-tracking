namespace BusTracking.Core.Routes;

public sealed record RouteStop(string StopId, string Name, int Order, GeoPoint Location);

/// <summary>
/// A route drawn in GeoJSON: its line and its stops, sorted by <see cref="RouteStop.Order"/>.
/// </summary>
public sealed record RouteDefinition(string RouteId, string Name, RoutePath Path, IReadOnlyList<RouteStop> Stops)
{
    /// <summary>
    /// The first stop at or ahead of <paramref name="point"/> along the route. A bus past the
    /// last stop is heading back to the first one, because buses start the route again.
    /// Returns <c>null</c> only when the route has no stops.
    /// </summary>
    public RouteStop? NextStop(GeoPoint point)
    {
        var busDistance = Path.DistanceAlong(point);
        return Stops.FirstOrDefault(stop => Path.DistanceAlong(stop.Location) >= busDistance)
            ?? Stops.FirstOrDefault();
    }
}
