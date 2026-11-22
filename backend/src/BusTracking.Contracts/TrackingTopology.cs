namespace BusTracking.Contracts;

/// <summary>
/// Names shared by every service that talks to RabbitMQ.
/// </summary>
public static class TrackingTopology
{
    public const string ExchangeName = "tracking.events";

    public static string LocationUpdatedRoutingKey(string routeId) => $"location.updated.{routeId}";
}
