namespace BusTracking.Contracts;

/// <summary>
/// Response of <c>POST /api/locations</c> when the location was published.
/// </summary>
public sealed record LocationAccepted(Guid EventId);
