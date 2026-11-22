using BusTracking.Contracts;
using RabbitMQ.Client;

namespace BusTracking.Messaging;

/// <summary>
/// Declares the exchanges and queues every service relies on. Declaring is idempotent, so each
/// publisher and consumer calls it when it opens a channel instead of depending on start order.
/// </summary>
public static class TopologyInitializer
{
    /// <summary>
    /// How many times a consumer may receive the same message. The quorum queue counts the
    /// deliveries; once a message is rejected this many times it moves to the dead letter queue.
    /// </summary>
    public const int MaxDeliveryAttempts = 3;

    public static Task DeclareAsync(IChannel channel, CancellationToken cancellationToken = default) =>
        channel.ExchangeDeclareAsync(
            TrackingTopology.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Declares a consumer's own durable quorum queue, bound to <c>tracking.events</c> with
    /// <paramref name="bindingPattern"/>, plus the shared dead letter exchange and queue.
    /// </summary>
    public static async Task DeclareConsumerQueueAsync(
        IChannel channel,
        string queueName,
        string bindingPattern,
        CancellationToken cancellationToken = default)
    {
        await DeclareAsync(channel, cancellationToken);
        await DeclareDeadLetterAsync(channel, cancellationToken);

        var arguments = new Dictionary<string, object?>
        {
            // Quorum queues are replicated and durable, and they count deliveries per message,
            // which gives a retry limit without writing retry logic.
            ["x-queue-type"] = "quorum",
            ["x-dead-letter-exchange"] = TrackingTopology.DeadLetterExchangeName,
            // The limit counts redeliveries, so the first delivery plus this many retries.
            ["x-delivery-limit"] = MaxDeliveryAttempts - 1,
        };

        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queueName,
            TrackingTopology.ExchangeName,
            bindingPattern,
            cancellationToken: cancellationToken);
    }

    private static async Task DeclareDeadLetterAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            TrackingTopology.DeadLetterExchangeName,
            ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            TrackingTopology.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            TrackingTopology.DeadLetterQueueName,
            TrackingTopology.DeadLetterExchangeName,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);
    }
}
