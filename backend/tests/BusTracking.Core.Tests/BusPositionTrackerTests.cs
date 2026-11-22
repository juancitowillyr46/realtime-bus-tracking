using BusTracking.Contracts;

namespace BusTracking.Core.Tests;

public class BusPositionTrackerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private static BusPosition Position(string busId, DateTimeOffset recordedAt, string routeId = "route-01") =>
        new(Guid.NewGuid(), busId, routeId, 4.81, -75.70, 25, 270, recordedAt, "stop-01", "Parada A");

    [Fact]
    public void TryUpdate_FirstPosition_IsAccepted()
    {
        var tracker = new BusPositionTracker();

        Assert.True(tracker.TryUpdate(Position("bus-01", Now)));
    }

    [Fact]
    public void TryUpdate_NewerPosition_ReplacesTheCurrentOne()
    {
        var tracker = new BusPositionTracker();
        tracker.TryUpdate(Position("bus-01", Now));
        var newer = Position("bus-01", Now.AddSeconds(5));

        Assert.True(tracker.TryUpdate(newer));
        Assert.Equal(newer, Assert.Single(tracker.GetByRoute("route-01")));
    }

    [Fact]
    public void TryUpdate_OlderOrEqualPosition_IsRejected()
    {
        var tracker = new BusPositionTracker();
        var current = Position("bus-01", Now);
        tracker.TryUpdate(current);

        Assert.False(tracker.TryUpdate(Position("bus-01", Now.AddSeconds(-5))));
        Assert.False(tracker.TryUpdate(Position("bus-01", Now)));
        Assert.Equal(current, Assert.Single(tracker.GetByRoute("route-01")));
    }

    [Fact]
    public void TryUpdate_TracksEachBusSeparately()
    {
        var tracker = new BusPositionTracker();
        tracker.TryUpdate(Position("bus-01", Now));

        // Older than bus-01's position, but bus-02 has none yet.
        Assert.True(tracker.TryUpdate(Position("bus-02", Now.AddSeconds(-30))));
        Assert.Equal(["bus-01", "bus-02"], tracker.GetByRoute("route-01").Select(position => position.BusId));
    }

    [Fact]
    public void GetByRoute_OnlyReturnsBusesOnThatRoute()
    {
        var tracker = new BusPositionTracker();
        tracker.TryUpdate(Position("bus-01", Now, "route-01"));
        tracker.TryUpdate(Position("bus-02", Now, "route-02"));

        Assert.Equal("bus-02", Assert.Single(tracker.GetByRoute("route-02")).BusId);
        Assert.Empty(tracker.GetByRoute("route-03"));
    }
}
