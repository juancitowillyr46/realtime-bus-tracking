using BusTracking.Contracts;

namespace BusTracking.Core;

public static class FleetStatus
{
    /// <summary>A bus that reported within this window is in use.</summary>
    public static readonly TimeSpan InUseWindow = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Every bus of the configured fleet plus any other bus that has reported, sorted by id.
    /// Fleet buses that never reported are free and have no route or last-seen time.
    /// </summary>
    public static IReadOnlyList<BusStatus> Build(
        IEnumerable<string> fleetBusIds,
        IEnumerable<BusPosition> latestPositions,
        DateTimeOffset now)
    {
        var latest = latestPositions.ToDictionary(position => position.BusId, StringComparer.Ordinal);

        return fleetBusIds
            .Concat(latest.Keys)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(busId => latest.TryGetValue(busId, out var position)
                ? new BusStatus(busId, now - position.RecordedAt <= InUseWindow, position.RouteId, position.RecordedAt)
                : new BusStatus(busId, InUse: false, RouteId: null, LastSeenAt: null))
            .ToList();
    }
}
