using BusTracking.Core.Routes;

namespace BusTracking.Core.Tests.Routes;

public class RouteDefinitionTests
{
    // East along the equator, then north: Start -> Corner -> End.
    private static readonly GeoPoint Start = new(0, 0);
    private static readonly GeoPoint Corner = new(0, 0.01);
    private static readonly GeoPoint End = new(0.01, 0.01);

    private static readonly RouteStop First = new("stop-01", "Parada A", 1, Start);
    private static readonly RouteStop Second = new("stop-02", "Parada B", 2, Corner);

    // The last stop sits before the end of the line, so a bus can be past it.
    private static readonly RouteStop Third = new("stop-03", "Parada C", 3, new GeoPoint(0.005, 0.01));

    private static RouteDefinition CreateRoute(params RouteStop[] stops) =>
        new("route-test", "Ruta de prueba", new RoutePath([Start, Corner, End]), stops);

    [Fact]
    public void NextStop_BeforeTheFirstStop_IsTheFirstStop()
    {
        var route = CreateRoute(First, Second, Third);

        Assert.Equal(First, route.NextStop(new GeoPoint(0, -0.001)));
    }

    [Fact]
    public void NextStop_BetweenStops_IsTheOneAhead()
    {
        var route = CreateRoute(First, Second, Third);

        Assert.Equal(Second, route.NextStop(new GeoPoint(0, 0.005)));
        Assert.Equal(Third, route.NextStop(new GeoPoint(0.002, 0.01)));
    }

    [Fact]
    public void NextStop_AtAStop_IsThatStop()
    {
        var route = CreateRoute(First, Second, Third);

        Assert.Equal(Second, route.NextStop(Corner));
    }

    [Fact]
    public void NextStop_PastTheLastStop_WrapsToTheFirstStop()
    {
        var route = CreateRoute(First, Second, Third);

        Assert.Equal(First, route.NextStop(new GeoPoint(0.008, 0.01)));
    }

    [Fact]
    public void NextStop_RouteWithoutStops_IsNull()
    {
        var route = CreateRoute();

        Assert.Null(route.NextStop(Corner));
    }

    [Fact]
    public void NextStop_OffTheRoute_IsNull()
    {
        var route = CreateRoute(First, Second, Third);

        // About 220 m north of the first segment, beyond the 150 m threshold.
        var offRoute = new GeoPoint(0.002, 0.005);

        Assert.True(route.IsOffRoute(offRoute));
        Assert.Null(route.NextStop(offRoute));
    }

    [Fact]
    public void IsOffRoute_WithinTheThreshold_IsFalse()
    {
        var route = CreateRoute(First, Second, Third);

        // About 110 m from the line: a normal GPS error in a city.
        Assert.False(route.IsOffRoute(new GeoPoint(0.001, 0.005)));
    }
}
