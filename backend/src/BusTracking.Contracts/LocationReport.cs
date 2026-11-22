namespace BusTracking.Contracts;

/// <summary>
/// Body of <c>POST /api/locations</c>: a position sent by a bus (simulated or a real phone).
/// </summary>
public sealed record LocationReport(
    string BusId,
    string RouteId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Heading,
    DateTimeOffset RecordedAt);
