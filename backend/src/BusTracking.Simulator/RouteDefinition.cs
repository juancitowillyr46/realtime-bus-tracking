using BusTracking.Core;

namespace BusTracking.Simulator;

public sealed record RouteStop(string StopId, string Name, int Order, GeoPoint Location);

public sealed record RouteDefinition(string RouteId, string Name, RoutePath Path, IReadOnlyList<RouteStop> Stops);
