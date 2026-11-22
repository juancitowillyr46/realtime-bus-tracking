namespace BusTracking.Core.Tests;

public class RecentEventIdsTests
{
    [Fact]
    public void TryAdd_NewId_ReturnsTrue()
    {
        var ids = new RecentEventIds();

        Assert.True(ids.TryAdd(Guid.NewGuid()));
    }

    [Fact]
    public void TryAdd_RepeatedId_ReturnsFalse()
    {
        var ids = new RecentEventIds();
        var eventId = Guid.NewGuid();
        ids.TryAdd(eventId);

        Assert.False(ids.TryAdd(eventId));
        Assert.True(ids.Contains(eventId));
    }

    [Fact]
    public void TryAdd_PastCapacity_ForgetsTheOldestId()
    {
        var ids = new RecentEventIds(capacity: 2);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();

        ids.TryAdd(first);
        ids.TryAdd(second);
        ids.TryAdd(third);

        Assert.False(ids.Contains(first));
        Assert.True(ids.Contains(second));
        Assert.True(ids.Contains(third));
    }

    [Fact]
    public void Constructor_CapacityBelowOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecentEventIds(capacity: 0));
    }
}
