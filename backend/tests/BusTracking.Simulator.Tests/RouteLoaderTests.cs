using BusTracking.Core;

namespace BusTracking.Simulator.Tests;

public class RouteLoaderTests
{
    private const string ValidRoute = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "kind": "stop", "stopId": "stop-02", "name": "Parada B", "order": 2 },
              "geometry": { "type": "Point", "coordinates": [-75.70, 4.81] }
            },
            {
              "type": "Feature",
              "properties": { "kind": "route", "routeId": "route-test", "name": "Ruta de prueba" },
              "geometry": { "type": "LineString", "coordinates": [[-75.69, 4.80], [-75.70, 4.81]] }
            },
            {
              "type": "Feature",
              "properties": { "kind": "stop", "stopId": "stop-01", "name": "Parada A", "order": "1" },
              "geometry": { "type": "Point", "coordinates": [-75.69, 4.80] }
            }
          ]
        }
        """;

    [Fact]
    public void Parse_ValidRoute_ReadsIdNameAndPath()
    {
        var route = RouteLoader.Parse(ValidRoute, "test.geojson");

        Assert.Equal("route-test", route.RouteId);
        Assert.Equal("Ruta de prueba", route.Name);
        Assert.Equal(2, route.Path.Points.Count);
    }

    [Fact]
    public void Parse_SwapsGeoJsonLongitudeLatitudeOrder()
    {
        var route = RouteLoader.Parse(ValidRoute, "test.geojson");

        Assert.Equal(new GeoPoint(4.80, -75.69), route.Path.Points[0]);
    }

    [Fact]
    public void Parse_SortsStopsByOrder_AcceptingNumbersStoredAsText()
    {
        var route = RouteLoader.Parse(ValidRoute, "test.geojson");

        Assert.Equal(new[] { "stop-01", "stop-02" }, route.Stops.Select(stop => stop.StopId).ToArray());
        Assert.Equal(new GeoPoint(4.80, -75.69), route.Stops[0].Location);
    }

    [Fact]
    public void Parse_WithoutLineString_Throws()
    {
        const string geoJson = """
            {
              "type": "FeatureCollection",
              "features": [
                { "type": "Feature", "properties": {}, "geometry": { "type": "Point", "coordinates": [-75.69, 4.80] } }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(() => RouteLoader.Parse(geoJson, "empty.geojson"));
        Assert.Contains("exactly one LineString", exception.Message);
    }

    [Fact]
    public void Parse_WithTwoLineStrings_Throws()
    {
        const string line = """
            { "type": "Feature", "properties": { "routeId": "r" }, "geometry": { "type": "LineString", "coordinates": [[-75.69, 4.80], [-75.70, 4.81]] } }
            """;
        var geoJson = $$"""{ "type": "FeatureCollection", "features": [{{line}}, {{line}}] }""";

        Assert.Throws<InvalidDataException>(() => RouteLoader.Parse(geoJson, "two-lines.geojson"));
    }

    [Fact]
    public void Parse_WithoutRouteId_Throws()
    {
        const string geoJson = """
            {
              "type": "FeatureCollection",
              "features": [
                { "type": "Feature", "properties": {}, "geometry": { "type": "LineString", "coordinates": [[-75.69, 4.80], [-75.70, 4.81]] } }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(() => RouteLoader.Parse(geoJson, "no-id.geojson"));
        Assert.Contains("routeId", exception.Message);
    }

    [Fact]
    public void LoadDirectory_ReadsEveryGeoJsonFile()
    {
        var directory = Directory.CreateTempSubdirectory("routes-");
        try
        {
            File.WriteAllText(Path.Combine(directory.FullName, "route-test.geojson"), ValidRoute);
            File.WriteAllText(Path.Combine(directory.FullName, "notes.txt"), "ignored");

            var routes = RouteLoader.LoadDirectory(directory.FullName);

            Assert.Equal("route-test", Assert.Single(routes).RouteId);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoadDirectory_MissingDirectory_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        Assert.Throws<DirectoryNotFoundException>(() => RouteLoader.LoadDirectory(missing));
    }
}
