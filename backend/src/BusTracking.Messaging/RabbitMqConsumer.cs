using System.Text.Json;
using BusTracking.Contracts;
using BusTracking.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BusTracking.Messaging;

/// <summary>
/// Base for every consumer: its own durable queue, manual acks, a bounded prefetch,
/// idempotency by <c>eventId</c> and a dead letter queue for messages that keep failing.
/// </summary>
/// <remarks>
/// Outcome of each delivery:
/// <list type="bullet">
/// <item>handled: ack;</item>
/// <item>already handled (same <c>MessageId</c>): ack without handling it again;</item>
/// <item>the handler throws: reject with requeue, so it is retried until the queue's delivery
/// limit moves it to <c>tracking.dead-letter</c>;</item>
/// <item>unreadable (no <c>MessageId</c> or invalid JSON): reject without requeue, straight to
/// <c>tracking.dead-letter</c>, since retrying would never succeed.</item>
/// </list>
/// </remarks>
public abstract class RabbitMqConsumer<T> : BackgroundService
    where T : class
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RecentEventIds _processed = new();
    private IChannel? _channel;

    protected RabbitMqConsumer(RabbitMqConnectionProvider connectionProvider, ILogger logger)
    {
        _connectionProvider = connectionProvider;
        Logger = logger;
    }

    protected abstract string QueueName { get; }

    protected abstract string BindingPattern { get; }

    /// <summary>How many unacknowledged messages the broker sends this consumer at once.</summary>
    protected virtual ushort PrefetchCount => 10;

    protected ILogger Logger { get; }

    protected abstract Task HandleAsync(T message, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ may still be starting; keep trying instead of stopping the whole process.
        // After the first connection, the client's automatic recovery restores the consumer.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumingAsync(stoppingToken);
                break;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.LogWarning(
                    exception,
                    "Could not start consuming {Queue}; retrying in {Delay}",
                    QueueName,
                    RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }
    }

    private async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await TopologyInitializer.DeclareConsumerQueueAsync(channel, QueueName, BindingPattern, cancellationToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnReceivedAsync(channel, delivery, cancellationToken);
        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken);

        _channel = channel;
        Logger.LogInformation("Consuming {Queue} bound to {Pattern}", QueueName, BindingPattern);
    }

    private async Task OnReceivedAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var eventId)
            || !TryDeserialize(delivery.Body, out var message))
        {
            Logger.LogError(
                "Unreadable message {MessageId} on {Queue}; sending it to the dead letter queue",
                delivery.BasicProperties.MessageId,
                QueueName);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken);
            return;
        }

        if (_processed.Contains(eventId))
        {
            Logger.LogInformation("Skipping duplicate event {EventId} on {Queue}", eventId, QueueName);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
            return;
        }

        try
        {
            await HandleAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogError(exception, "Failed to handle event {EventId} on {Queue}; it will be retried", eventId, QueueName);

            // basic.reject, not basic.nack: since RabbitMQ 4.3 only a reject counts as a failed
            // delivery towards the queue's x-delivery-limit; a nack would be retried forever.
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: true, cancellationToken);
            return;
        }

        // Recorded only after success, so a failed attempt can still be retried.
        _processed.TryAdd(eventId);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
    }

    private static bool TryDeserialize(ReadOnlyMemory<byte> body, out T message)
    {
        try
        {
            message = JsonSerializer.Deserialize<T>(body.Span, TrackingJson.Options)!;
            return message is not null;
        }
        catch (JsonException)
        {
            message = null!;
            return false;
        }
    }
}
