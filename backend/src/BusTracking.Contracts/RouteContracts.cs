namespace BusTracking.Contracts;

/// <summary>
/// An item of <c>GET /api/routes</c>.
/// </summary>
public sealed record RouteSummary(string RouteId, string Name);

/// <summary>
/// Response of <c>GET /api/routes/{routeId}</c>: everything the app needs to draw a route.
/// </summary>
public sealed record RouteDetails(
    string RouteId,
    string Name,
    IReadOnlyList<RoutePathPoint> Path,
    IReadOnlyList<RouteStopDetails> Stops);

public sealed record RoutePathPoint(double Latitude, double Longitude);

public sealed record RouteStopDetails(string StopId, string Name, int Order, double Latitude, double Longitude);
