using BusTracking.Core;

namespace BusTracking.Simulator.Tests;

public class SimulatedBusTests
{
    // About 1 112 m going east along the equator.
    private static readonly RouteDefinition Route = new(
        "route-test",
        "Ruta de prueba",
        new RoutePath([new GeoPoint(0, 0), new GeoPoint(0, 0.01)]),
        []);

    [Fact]
    public void Advance_MovesSpeedTimesElapsedTime()
    {
        var bus = new SimulatedBus("bus-test", Route, 36, 0);

        // 36 km/h = 10 m/s, so 10 s = 100 m.
        bus.Advance(TimeSpan.FromSeconds(10), 36);

        Assert.Equal(100, bus.DistanceMeters, 1e-9);
        Assert.Equal(36.0, bus.CurrentSpeedKmh);
    }

    [Fact]
    public void Advance_PastTheEnd_StartsTheRouteAgain()
    {
        var bus = new SimulatedBus("bus-test", Route, 36, Route.Path.TotalLengthMeters - 20);

        bus.Advance(TimeSpan.FromSeconds(10), 36);

        Assert.Equal(80, bus.DistanceMeters, 1e-6);
    }

    [Fact]
    public void Constructor_OffsetLongerThanTheRoute_Wraps()
    {
        var bus = new SimulatedBus("bus-test", Route, 36, Route.Path.TotalLengthMeters + 50);

        Assert.Equal(50, bus.DistanceMeters, 1e-6);
    }

    [Fact]
    public void NextSpeed_StaysWithinTheVariationRange()
    {
        var bus = new SimulatedBus("bus-test", Route, 25, 0);
        var random = new Random(42);

        for (var i = 0; i < 1_000; i++)
        {
            Assert.InRange(bus.NextSpeed(random), 20, 30);
        }
    }

    [Fact]
    public void NextSpeed_NeverGoesBelowTheMinimum()
    {
        var bus = new SimulatedBus("bus-test", Route, 3, 0);
        var random = new Random(42);

        for (var i = 0; i < 1_000; i++)
        {
            Assert.True(bus.NextSpeed(random) >= SimulatedBus.MinimumSpeedKmh);
        }
    }
}
