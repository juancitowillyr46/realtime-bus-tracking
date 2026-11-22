using BusTracking.Contracts;
using RabbitMQ.Client;

namespace BusTracking.Messaging;

/// <summary>
/// Declares the exchanges every service relies on. Declaring is idempotent, so each
/// publisher and consumer calls it when it opens a channel instead of depending on start order.
/// </summary>
public static class TopologyInitializer
{
    public static Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default) =>
        channel.ExchangeDeclareAsync(
            TrackingTopology.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
}
