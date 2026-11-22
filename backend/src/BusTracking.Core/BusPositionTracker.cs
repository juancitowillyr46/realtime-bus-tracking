using BusTracking.Contracts;

namespace BusTracking.Core;

/// <summary>
/// The latest known position of each bus. Positions can arrive out of order (retries,
/// redeliveries), so one recorded earlier than what is already known is rejected.
/// </summary>
public sealed class BusPositionTracker
{
    private readonly Dictionary<string, BusPosition> _latest = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    /// <summary>
    /// Stores <paramref name="position"/> if it is newer than the bus's current one.
    /// Returns <c>false</c> for a position recorded at or before the current one.
    /// </summary>
    public bool TryUpdate(BusPosition position)
    {
        lock (_lock)
        {
            if (_latest.TryGetValue(position.BusId, out var current) && position.RecordedAt <= current.RecordedAt)
            {
                return false;
            }

            _latest[position.BusId] = position;
            return true;
        }
    }

    /// <summary>The current position of every bus whose latest position is on <paramref name="routeId"/>.</summary>
    public IReadOnlyList<BusPosition> GetByRoute(string routeId)
    {
        lock (_lock)
        {
            return _latest.Values
                .Where(position => position.RouteId == routeId)
                .OrderBy(position => position.BusId, StringComparer.Ordinal)
                .ToList();
        }
    }
}
