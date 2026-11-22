namespace BusTracking.Contracts;

/// <summary>
/// The SignalR hub the app connects to: its path, the methods it calls and the events it receives.
/// </summary>
public static class TrackingHubContract
{
    public const string Path = "/hubs/tracking";

    /// <summary>Client → server. Subscribes to a route and returns its buses' current positions.</summary>
    public const string JoinRoute = "JoinRoute";

    /// <summary>Client → server. Stops receiving a route's positions.</summary>
    public const string LeaveRoute = "LeaveRoute";

    /// <summary>Server → client, with a <see cref="BusPosition"/>.</summary>
    public const string BusMoved = "busMoved";

    public static string RouteGroup(string routeId) => $"route:{routeId}";
}
