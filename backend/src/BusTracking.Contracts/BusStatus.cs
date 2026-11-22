namespace BusTracking.Contracts;

/// <summary>
/// An item of <c>GET /api/buses</c>. A bus is in use while it keeps reporting positions, so the
/// driver app can grey it out instead of letting two phones (or a phone and the simulator) send
/// positions for the same bus.
/// </summary>
public sealed record BusStatus(string BusId, bool InUse, string? RouteId, DateTimeOffset? LastSeenAt);
