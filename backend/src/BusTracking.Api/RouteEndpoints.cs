using BusTracking.Contracts;
using BusTracking.Core.Routes;

namespace BusTracking.Api;

public static class RouteEndpoints
{
    /// <summary>
    /// Loads the routes once at startup. <c>Routes:Directory</c> overrides the default
    /// <c>data/routes</c> found by walking up from the executable.
    /// </summary>
    public static IServiceCollection AddRouteCatalog(this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var configured = provider.GetRequiredService<IConfiguration>()["Routes:Directory"];
            var directory = RoutesDirectoryLocator.Resolve(configured, AppContext.BaseDirectory);
            return new RouteCatalog(RouteLoader.LoadDirectory(directory));
        });
        return services;
    }

    public static IEndpointRouteBuilder MapRouteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/routes", (RouteCatalog catalog) =>
            catalog.All
                .OrderBy(route => route.RouteId, StringComparer.Ordinal)
                .Select(route => new RouteSummary(route.RouteId, route.Name))
                .ToList());

        app.MapGet("/api/routes/{routeId}", (string routeId, RouteCatalog catalog) =>
        {
            var route = catalog.Find(routeId);
            if (route is null)
            {
                return Results.Problem(
                    title: $"La ruta '{routeId}' no existe.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(ToDetails(route));
        });

        return app;
    }

    private static RouteDetails ToDetails(RouteDefinition route) =>
        new(
            route.RouteId,
            route.Name,
            route.Path.Points.Select(point => new RoutePathPoint(point.Latitude, point.Longitude)).ToList(),
            route.Stops
                .Select(stop => new RouteStopDetails(
                    stop.StopId,
                    stop.Name,
                    stop.Order,
                    stop.Location.Latitude,
                    stop.Location.Longitude))
                .ToList());
}
