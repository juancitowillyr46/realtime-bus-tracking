namespace BusTracking.Core;

/// <summary>
/// A WGS 84 position in decimal degrees. Note that GeoJSON stores it the other way round: [longitude, latitude].
/// </summary>
public readonly record struct GeoPoint(double Latitude, double Longitude);
