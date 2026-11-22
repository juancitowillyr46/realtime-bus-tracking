using BusTracking.Core;

namespace BusTracking.Api;

public static class FleetEndpoints
{
    /// <summary>
    /// <c>GET /api/buses</c>: the fleet from <c>Fleet:BusIds</c> (fictitious ids, until the buses
    /// CRUD of phase 5) and whether each bus is in use right now.
    /// </summary>
    public static IEndpointRouteBuilder MapFleetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/buses", (IConfiguration configuration, BusPositionTracker tracker, TimeProvider clock) =>
        {
            var fleet = configuration.GetSection("Fleet:BusIds").Get<string[]>() ?? [];
            return FleetStatus.Build(fleet, tracker.GetAll(), clock.GetUtcNow());
        });

        return app;
    }
}
