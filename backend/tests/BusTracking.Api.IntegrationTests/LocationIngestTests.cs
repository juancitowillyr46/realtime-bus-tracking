using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BusTracking.Contracts;
using BusTracking.Messaging;
using RabbitMQ.Client;

namespace BusTracking.Api.IntegrationTests;

public class LocationIngestTests : IClassFixture<RabbitMqFixture>
{
    private const string RouteId = "route-test";

    private readonly RabbitMqFixture _rabbitMq;

    public LocationIngestTests(RabbitMqFixture rabbitMq)
    {
        _rabbitMq = rabbitMq;
    }

    [Fact]
    public async Task PostValidLocation_PublishesOneLocationUpdated()
    {
        await using var factory = _rabbitMq.CreateApiFactory();
        using var client = factory.CreateClient();
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var queue = await BindTemporaryQueueAsync(channel, "location.updated.*");

        var report = new LocationReport("bus-test", RouteId, 4.8133, -75.6961, 25, 270, DateTimeOffset.UtcNow);
        var response = await client.PostAsJsonAsync("/api/locations", report);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var accepted = await response.Content.ReadFromJsonAsync<LocationAccepted>();
        Assert.NotNull(accepted);

        var message = await WaitForMessageAsync(channel, queue, TimeSpan.FromSeconds(10));
        Assert.NotNull(message);
        Assert.Equal($"location.updated.{RouteId}", message.RoutingKey);
        Assert.Equal(accepted.EventId.ToString(), message.BasicProperties.MessageId);
        Assert.Equal(nameof(LocationUpdated), message.BasicProperties.Type);
        Assert.Equal("application/json", message.BasicProperties.ContentType);
        Assert.Equal(DeliveryModes.Persistent, message.BasicProperties.DeliveryMode);

        var published = JsonSerializer.Deserialize<LocationUpdated>(message.Body.Span, TrackingJson.Options);
        Assert.NotNull(published);
        Assert.Equal(accepted.EventId, published.EventId);
        Assert.Equal(report.BusId, published.BusId);
        Assert.Equal(report.Latitude, published.Latitude);
        Assert.Equal(report.Longitude, published.Longitude);

        // Exactly one message per accepted location.
        Assert.Null(await channel.BasicGetAsync(queue, autoAck: true));
    }

    [Fact]
    public async Task PostInvalidLocation_Returns400AndPublishesNothing()
    {
        await using var factory = _rabbitMq.CreateApiFactory();
        using var client = factory.CreateClient();
        await using var connection = await _rabbitMq.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var queue = await BindTemporaryQueueAsync(channel, "#");

        var report = new LocationReport("", RouteId, 120, -75.6961, 25, 270, DateTimeOffset.UtcNow);
        var response = await client.PostAsJsonAsync("/api/locations", report);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(await WaitForMessageAsync(channel, queue, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public async Task GetHealth_WithRabbitMqRunning_Returns200()
    {
        await using var factory = _rabbitMq.CreateApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> BindTemporaryQueueAsync(IChannel channel, string pattern)
    {
        await TopologyInitializer.DeclareAsync(channel);
        var queue = await channel.QueueDeclareAsync(queue: string.Empty, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue.QueueName, TrackingTopology.ExchangeName, pattern);
        return queue.QueueName;
    }

    private static async Task<BasicGetResult?> WaitForMessageAsync(IChannel channel, string queue, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var message = await channel.BasicGetAsync(queue, autoAck: true);
            if (message is not null)
            {
                return message;
            }

            await Task.Delay(100);
        }

        return null;
    }
}
