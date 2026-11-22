namespace BusTracking.Contracts;

/// <summary>
/// A bus's latest position as the app receives it over SignalR: the <see cref="LocationUpdated"/>
/// fields plus the stop the bus is heading to (null when the route is unknown or has no stops).
/// </summary>
public sealed record BusPosition(
    Guid EventId,
    string BusId,
    string RouteId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Heading,
    DateTimeOffset RecordedAt,
    string? NextStopId,
    string? NextStopName);
