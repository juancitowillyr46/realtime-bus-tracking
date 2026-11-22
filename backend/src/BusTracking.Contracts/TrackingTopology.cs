namespace BusTracking.Contracts;

/// <summary>
/// Names shared by every service that talks to RabbitMQ.
/// </summary>
public static class TrackingTopology
{
    public const string ExchangeName = "tracking.events";

    /// <summary>Receives messages a consumer gave up on (failed too many times or unreadable).</summary>
    public const string DeadLetterExchangeName = "tracking.dlx";

    /// <summary>Where dead-lettered messages wait to be inspected in the management UI.</summary>
    public const string DeadLetterQueueName = "tracking.dead-letter";

    /// <summary>The realtime consumer's queue: every location, pushed to the app over SignalR.</summary>
    public const string RealtimeLocationQueue = "realtime.location";

    /// <summary>Binding pattern that matches <see cref="LocationUpdatedRoutingKey"/> for every route.</summary>
    public const string AllLocationUpdatesPattern = "location.updated.*";

    public static string LocationUpdatedRoutingKey(string routeId) => $"location.updated.{routeId}";
}
