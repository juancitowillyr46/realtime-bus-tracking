using BusTracking.Contracts;

namespace BusTracking.Core.Tests;

public class FleetStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private static BusPosition Position(string busId, DateTimeOffset recordedAt) =>
        new(Guid.NewGuid(), busId, "route-01", 4.81, -75.70, 25, 270, recordedAt, null, null, 0);

    [Fact]
    public void FleetBusThatNeverReported_IsFree()
    {
        var status = Assert.Single(FleetStatus.Build(["bus-01"], [], Now));

        Assert.Equal(new BusStatus("bus-01", false, null, null), status);
    }

    [Fact]
    public void BusThatReportedWithinTheWindow_IsInUse()
    {
        var recordedAt = Now.AddSeconds(-20);

        var status = Assert.Single(FleetStatus.Build(["bus-01"], [Position("bus-01", recordedAt)], Now));

        Assert.True(status.InUse);
        Assert.Equal("route-01", status.RouteId);
        Assert.Equal(recordedAt, status.LastSeenAt);
    }

    [Fact]
    public void BusThatStoppedReporting_IsFreeAgain()
    {
        var status = Assert.Single(FleetStatus.Build(["bus-01"], [Position("bus-01", Now.AddMinutes(-2))], Now));

        Assert.False(status.InUse);
    }

    [Fact]
    public void BusOutsideTheFleet_IsListedToo_SortedById()
    {
        var statuses = FleetStatus.Build(["bus-02", "bus-01"], [Position("bus-09", Now)], Now);

        Assert.Equal(["bus-01", "bus-02", "bus-09"], statuses.Select(status => status.BusId));
        Assert.True(statuses[2].InUse);
    }
}
