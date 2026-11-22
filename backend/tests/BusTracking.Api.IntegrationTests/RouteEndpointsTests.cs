using System.Net;
using System.Net.Http.Json;
using BusTracking.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BusTracking.Api.IntegrationTests;

/// <summary>
/// The routes endpoints only read GeoJSON files, so these tests need no RabbitMQ.
/// They use their own route file instead of the one in data/routes, which will change.
/// </summary>
public sealed class RouteEndpointsTests : IDisposable
{
    private const string TestRoute = """
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

    private readonly string _routesDirectory = Directory.CreateTempSubdirectory("bus-tracking-routes-").FullName;
    private readonly WebApplicationFactory<Program> _factory;

    public RouteEndpointsTests()
    {
        File.WriteAllText(Path.Combine(_routesDirectory, "route-test.geojson"), TestRoute);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("Routes:Directory", _routesDirectory));
    }

    public void Dispose()
    {
        _factory.Dispose();
        Directory.Delete(_routesDirectory, recursive: true);
    }

    [Fact]
    public async Task GetRoutes_ListsEveryRoute()
    {
        using var client = _factory.CreateClient();

        var routes = await client.GetFromJsonAsync<List<RouteSummary>>("/api/routes", TrackingJson.Options);

        var route = Assert.Single(routes!);
        Assert.Equal(new RouteSummary("route-test", "Ruta de prueba"), route);
    }

    [Fact]
    public async Task GetRoute_ReturnsPathAndStopsInOrder()
    {
        using var client = _factory.CreateClient();

        var route = await client.GetFromJsonAsync<RouteDetails>("/api/routes/route-test", TrackingJson.Options);

        Assert.NotNull(route);
        Assert.Equal(3, route.Path.Count);
        Assert.Equal(new RoutePathPoint(4.80, -75.69), route.Path[0]);
        Assert.Equal(["stop-01", "stop-02"], route.Stops.Select(stop => stop.StopId));
        Assert.Equal(new RouteStopDetails("stop-01", "Parada A", 1, 4.80, -75.69), route.Stops[0]);
    }

    [Fact]
    public async Task GetRoute_UnknownRoute_Returns404WithSpanishMessage()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/routes/route-missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("no existe", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetRoute_ResponseIsCamelCase()
    {
        using var client = _factory.CreateClient();

        var json = await client.GetStringAsync("/api/routes/route-test");

        Assert.Contains("\"routeId\":\"route-test\"", json);
        Assert.Contains("\"stopId\":\"stop-01\"", json);
    }
}
