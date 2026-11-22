using System.Globalization;
using System.Text.Json;
using BusTracking.Contracts;
using BusTracking.Messaging;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BusTracking.EventTail;

/// <param name="BindingPattern">Topic pattern to listen to: <c>#</c> means every event.</param>
public sealed record EventTailSettings(string BindingPattern);

/// <summary>
/// Debugging tool: binds a temporary queue to the tracking exchange and prints every message.
/// </summary>
public sealed class EventTailWorker : BackgroundService
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly EventTailSettings _settings;
    private readonly TimeProvider _clock;

    public EventTailWorker(RabbitMqConnectionProvider connectionProvider, EventTailSettings settings, TimeProvider clock)
    {
        _connectionProvider = connectionProvider;
        _settings = settings;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await _connectionProvider.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await TopologyInitializer.DeclareAsync(channel, stoppingToken);

        // Exclusive, auto-delete queue with a server-generated name: it disappears when the tail stops.
        var queue = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue.QueueName,
            TrackingTopology.ExchangeName,
            _settings.BindingPattern,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) =>
        {
            Print(delivery);
            return Task.CompletedTask;
        };

        // autoAck is fine for an inspection tool that only prints. Real consumers (phase 2 onwards)
        // ack manually, after processing, so a crash never loses a message.
        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer: consumer, cancellationToken: stoppingToken);

        Console.WriteLine($"Listening on '{TrackingTopology.ExchangeName}' with pattern '{_settings.BindingPattern}'. Ctrl+C to stop.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private void Print(BasicDeliverEventArgs delivery)
    {
        var now = _clock.GetUtcNow();

        if (delivery.BasicProperties.Type == nameof(LocationUpdated))
        {
            try
            {
                var location = JsonSerializer.Deserialize<LocationUpdated>(delivery.Body.Span, TrackingJson.Options);
                if (location is not null)
                {
                    var latencyMs = (now - location.RecordedAt).TotalMilliseconds;
                    Console.WriteLine(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{now:HH:mm:ss.fff}  {delivery.RoutingKey,-28} {location.BusId,-8} {location.Latitude,10:F5} {location.Longitude,11:F5} {location.SpeedKmh,5:F1} km/h  latency {latencyMs,6:F0} ms"));
                    return;
                }
            }
            catch (JsonException exception)
            {
                Console.WriteLine($"{now:HH:mm:ss.fff}  {delivery.RoutingKey}  invalid JSON: {exception.Message}");
                return;
            }
        }

        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{now:HH:mm:ss.fff}  {delivery.RoutingKey}  {delivery.BasicProperties.Type ?? "(no type)"}  {delivery.Body.Length} bytes"));
    }
}
