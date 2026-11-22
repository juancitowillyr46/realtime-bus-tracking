namespace BusTracking.Api.IntegrationTests;

/// <summary>
/// A temporary routes directory with one small route, so the tests do not depend on
/// data/routes, which will be redrawn. Deleted on dispose.
/// </summary>
public sealed class TestRoutes : IDisposable
{
    public const string RouteId = "route-test";

    private const string RouteGeoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "kind": "route", "routeId": "route-test", "name": "Ruta de prueba" },
              "geometry": { "type": "LineString", "coordinates": [[-75.69, 4.80], [-75.70, 4.81], [-75.71, 4.81]] }
            },
            {
              "type": "Feature",
              "properties": { "kind": "stop", "stopId": "stop-02", "name": "Parada B", "order": 2 },
              "geometry": { "type": "Point", "coordinates": [-75.71, 4.81] }
            },
            {
              "type": "Feature",
              "properties": { "kind": "stop", "stopId": "stop-01", "name": "Parada A", "order": 1 },
              "geometry": { "type": "Point", "coordinates": [-75.69, 4.80] }
            }
          ]
        }
        """;

    public TestRoutes()
    {
        Directory = System.IO.Directory.CreateTempSubdirectory("bus-tracking-routes-").FullName;
        File.WriteAllText(Path.Combine(Directory, $"{RouteId}.geojson"), RouteGeoJson);
    }

    public string Directory { get; }

    public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
}
