namespace BusTracking.Core.Tests;

public class GeoMathTests
{
    [Fact]
    public void DistanceMeters_OneDegreeOfLatitude_IsAbout111Kilometers()
    {
        var distance = GeoMath.DistanceMeters(new GeoPoint(0, 0), new GeoPoint(1, 0));

        Assert.Equal(111_195, distance, 1.0);
    }

    [Fact]
    public void DistanceMeters_SamePoint_IsZero()
    {
        var point = new GeoPoint(4.8133, -75.6961);

        Assert.Equal(0, GeoMath.DistanceMeters(point, point), 1e-9);
    }

    [Fact]
    public void BearingDegrees_TowardsNorth_IsZero()
    {
        var bearing = GeoMath.BearingDegrees(new GeoPoint(0, 0), new GeoPoint(1, 0));

        Assert.Equal(0, bearing, 1e-6);
    }

    [Fact]
    public void BearingDegrees_TowardsEast_Is90()
    {
        var bearing = GeoMath.BearingDegrees(new GeoPoint(0, 0), new GeoPoint(0, 1));

        Assert.Equal(90, bearing, 1e-6);
    }

    [Fact]
    public void BearingDegrees_TowardsWest_Is270()
    {
        var bearing = GeoMath.BearingDegrees(new GeoPoint(0, 0), new GeoPoint(0, -1));

        Assert.Equal(270, bearing, 1e-6);
    }
}
