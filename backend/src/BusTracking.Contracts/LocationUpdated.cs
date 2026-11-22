namespace BusTracking.Contracts;

/// <summary>
/// Published by the ingest API every time it accepts a bus location.
/// Routing key: <c>location.updated.{routeId}</c>.
/// </summary>
public sealed record LocationUpdated(
    Guid EventId,
    string BusId,
    string RouteId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Heading,
    DateTimeOffset RecordedAt);
