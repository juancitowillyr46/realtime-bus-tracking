using System.Text.Json;
using BusTracking.Contracts;
using RabbitMQ.Client;

namespace BusTracking.Messaging;

/// <summary>
/// Publishes persistent JSON messages with publisher confirms: <see cref="PublishAsync{T}"/>
/// only returns once the broker has taken responsibility for the message.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private static readonly CreateChannelOptions ChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    private readonly RabbitMqConnectionProvider _connectionProvider;

    // A single channel shared by all requests; publishes are serialized to keep it simple and safe.
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;

    public RabbitMqEventPublisher(RabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task PublishAsync<T>(T message, string routingKey, Guid messageId, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message, TrackingJson.Options);
        var properties = new BasicProperties
        {
            MessageId = messageId.ToString(),
            Type = typeof(T).Name,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
        };

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);

            // mandatory: false, because in phase 1 there may be no queue bound yet and that is not an error.
            await channel.BasicPublishAsync(
                TrackingTopology.ExchangeName,
                routingKey,
                false,
                properties,
                body,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new MessagePublishException(
                $"Could not publish {typeof(T).Name} with routing key '{routingKey}'.",
                exception);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        _lock.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(ChannelOptions, cancellationToken);
        await TopologyInitializer.DeclareAsync(channel, cancellationToken);

        _channel = channel;
        return channel;
    }
}
