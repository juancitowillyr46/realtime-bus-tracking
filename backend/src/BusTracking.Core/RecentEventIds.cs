namespace BusTracking.Core;

/// <summary>
/// Remembers the last <c>capacity</c> event ids a consumer processed, so a redelivered message
/// is recognised and skipped. It lives in memory, so it forgets on restart; phase 5 moves
/// idempotency to the database.
/// </summary>
public sealed class RecentEventIds
{
    private readonly int _capacity;
    private readonly HashSet<Guid> _ids = [];
    private readonly Queue<Guid> _order = new();
    private readonly Lock _lock = new();

    public RecentEventIds(int capacity = 10_000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    public bool Contains(Guid eventId)
    {
        lock (_lock)
        {
            return _ids.Contains(eventId);
        }
    }

    /// <summary>
    /// Records <paramref name="eventId"/>. Returns <c>false</c> when it was already recorded.
    /// When full, the oldest id is forgotten.
    /// </summary>
    public bool TryAdd(Guid eventId)
    {
        lock (_lock)
        {
            if (!_ids.Add(eventId))
            {
                return false;
            }

            _order.Enqueue(eventId);
            if (_order.Count > _capacity)
            {
                _ids.Remove(_order.Dequeue());
            }

            return true;
        }
    }
}
