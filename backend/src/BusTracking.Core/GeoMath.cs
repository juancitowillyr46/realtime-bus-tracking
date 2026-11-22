namespace BusTracking.Core;

public static class GeoMath
{
    public const double EarthRadiusMeters = 6_371_000;

    /// <summary>
    /// Great-circle distance between two points (haversine formula).
    /// </summary>
    public static double DistanceMeters(GeoPoint from, GeoPoint to)
    {
        var fromLatitude = ToRadians(from.Latitude);
        var toLatitude = ToRadians(to.Latitude);
        var deltaLatitude = toLatitude - fromLatitude;
        var deltaLongitude = ToRadians(to.Longitude - from.Longitude);

        var sinLatitude = Math.Sin(deltaLatitude / 2);
        var sinLongitude = Math.Sin(deltaLongitude / 2);
        var h = (sinLatitude * sinLatitude)
            + (Math.Cos(fromLatitude) * Math.Cos(toLatitude) * sinLongitude * sinLongitude);

        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>
    /// Initial bearing from one point to another, in degrees from 0 to 360 (0 = north, 90 = east).
    /// </summary>
    public static double BearingDegrees(GeoPoint from, GeoPoint to)
    {
        var fromLatitude = ToRadians(from.Latitude);
        var toLatitude = ToRadians(to.Latitude);
        var deltaLongitude = ToRadians(to.Longitude - from.Longitude);

        var y = Math.Sin(deltaLongitude) * Math.Cos(toLatitude);
        var x = (Math.Cos(fromLatitude) * Math.Sin(toLatitude))
            - (Math.Sin(fromLatitude) * Math.Cos(toLatitude) * Math.Cos(deltaLongitude));

        var degrees = ToDegrees(Math.Atan2(y, x));
        return (degrees + 360) % 360;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static double ToDegrees(double radians) => radians * 180 / Math.PI;
}
