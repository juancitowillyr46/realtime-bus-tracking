namespace BusTracking.Core.Tests;

public class RoutePathTests
{
    private static readonly GeoPoint Start = new(0, 0);
    private static readonly GeoPoint Middle = new(0, 0.01);
    private static readonly GeoPoint End = new(0.01, 0.01);

    [Fact]
    public void Constructor_WithLessThanTwoPoints_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RoutePath([Start]));
    }

    [Fact]
    public void TotalLengthMeters_IsTheSumOfItsSegments()
    {
        var path = new RoutePath([Start, Middle, End]);

        var expected = GeoMath.DistanceMeters(Start, Middle) + GeoMath.DistanceMeters(Middle, End);
        Assert.Equal(expected, path.TotalLengthMeters, 1e-6);
    }

    [Fact]
    public void PositionAt_Zero_IsTheFirstPoint()
    {
        var path = new RoutePath([Start, Middle, End]);

        Assert.Equal(Start, path.PositionAt(0).Point);
    }

    [Fact]
    public void PositionAt_TotalLength_IsTheLastPoint()
    {
        var path = new RoutePath([Start, Middle, End]);

        var point = path.PositionAt(path.TotalLengthMeters).Point;

        Assert.Equal(End.Latitude, point.Latitude, 1e-9);
        Assert.Equal(End.Longitude, point.Longitude, 1e-9);
    }

    [Fact]
    public void PositionAt_BeyondTheEnd_IsTheLastPoint()
    {
        var path = new RoutePath([Start, Middle, End]);

        var point = path.PositionAt(path.TotalLengthMeters + 500).Point;

        Assert.Equal(End.Latitude, point.Latitude, 1e-9);
        Assert.Equal(End.Longitude, point.Longitude, 1e-9);
    }

    [Fact]
    public void PositionAt_HalfOfASingleSegment_IsItsMidpoint()
    {
        var path = new RoutePath([Start, Middle]);

        var point = path.PositionAt(path.TotalLengthMeters / 2).Point;

        Assert.Equal(0, point.Latitude, 1e-9);
        Assert.Equal(0.005, point.Longitude, 1e-9);
    }

    [Fact]
    public void PositionAt_ReturnsTheHeadingOfTheCurrentSegment()
    {
        var path = new RoutePath([Start, Middle, End]);
        var firstSegment = GeoMath.DistanceMeters(Start, Middle);

        Assert.Equal(90, path.PositionAt(firstSegment / 2).HeadingDegrees, 1e-3);
        Assert.Equal(0, path.PositionAt(firstSegment + 10).HeadingDegrees, 1e-3);
    }
}
