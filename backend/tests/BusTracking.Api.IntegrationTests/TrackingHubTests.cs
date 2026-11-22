using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using BusTracking.Contracts;
using BusTracking.Messaging;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace BusTracking.Api.IntegrationTests;

/// <summary>
/// The whole realtime path inside the API: POST /api/locations → RabbitMQ → realtime.location
/// → RealtimeLocationConsumer → TrackingHub → a SignalR client.
/// </summary>
public sealed class TrackingHubTests : IClassFixture<RabbitMqFixture>, IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    // Halfway along the first segment of the test route, so the next stop is "Parada B".
    private const double Latitude = 4.805;
    private const double Longitude = -75.695;

    private readonly RabbitMqFixture _rabbitMq;
    private readonly TestRoutes _routes = new();

    public TrackingHubTests(RabbitMqFixture rabbitMq)
    {
        _rabbitMq = rabbitMq;
    }

    public void Dispose() => _routes.Dispose();

    [Fact]
    public async Task PostedLocation_ReachesClientsOfThatRouteWithTheNextStop()
    {
        await using var api = await StartApiAsync();
        await using var client = await api.ConnectAsync(TestRoutes.RouteId);

        var eventId = await api.PostLocationAsync("bus-01", DateTimeOffset.UtcNow);

        var position = await client.NextAsync();
        Assert.Equal(eventId, position.EventId);
        Assert.Equal("bus-01", position.BusId);
        Assert.Equal(TestRoutes.RouteId, position.RouteId);
        Assert.Equal(Latitude, position.Latitude);
        Assert.Equal("stop-02", position.NextStopId);
        Assert.Equal("Parada B", position.NextStopName);
    }

    [Fact]
    public async Task ClientOfAnotherRoute_ReceivesNothing()
    {
        await using var api = await StartApiAsync();
        await using var routeClient = await api.ConnectAsync(TestRoutes.RouteId);
        await using var otherClient = await api.ConnectAsync("route-other");

        await api.PostLocationAsync("bus-01", DateTimeOffset.UtcNow);
        await routeClient.NextAsync();

        // The route's client already got it; give the other client a moment to (wrongly) get it too.
        await Task.Delay(500);
        Assert.Empty(otherClient.Received);
    }

    [Fact]
    public async Task JoinRoute_ReturnsTheCurrentPositions()
    {
        await using var api = await StartApiAsync();
        await using var firstClient = await api.ConnectAsync(TestRoutes.RouteId);
        var eventId = await api.PostLocationAsync("bus-01", DateTimeOffset.UtcNow);
        await firstClient.NextAsync();

        await using var lateClient = await api.ConnectAsync(TestRoutes.RouteId);

        var current = Assert.Single(lateClient.InitialPositions);
        Assert.Equal(eventId, current.EventId);
    }

    [Fact]
    public async Task OlderPositionOfABus_IsNotPushed()
    {
        await using var api = await StartApiAsync();
        await using var client = await api.ConnectAsync(TestRoutes.RouteId);
        var now = DateTimeOffset.UtcNow;

        var newer = await api.PostLocationAsync("bus-01", now);
        await api.PostLocationAsync("bus-01", now.AddSeconds(-10));
        var newest = await api.PostLocationAsync("bus-01", now.AddSeconds(5));

        Assert.Equal(newer, (await client.NextAsync()).EventId);
        Assert.Equal(newest, (await client.NextAsync()).EventId);
    }

    private async Task<ApiUnderTest> StartApiAsync()
    {
        // Declare the consumer's queue before posting, so a message published while the
        // API's consumer is still starting waits in the queue instead of being dropped.
        await using (var connection = await _rabbitMq.CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync())
        {
            await TopologyInitializer.DeclareConsumerQueueAsync(
                channel,
                TrackingTopology.RealtimeLocationQueue,
                TrackingTopology.AllLocationUpdatesPattern);
        }

        return new ApiUnderTest(_rabbitMq.CreateApiFactory(_routes.Directory));
    }

    private sealed class ApiUnderTest : IAsyncDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _http;

        public ApiUnderTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _http = factory.CreateClient();
        }

        public async Task<HubClient> ConnectAsync(string routeId)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(_factory.Server.BaseAddress, TrackingHubContract.Path.TrimStart('/')), options =>
                {
                    // The in-memory test server has no real sockets; long polling goes through its handler.
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                })
                .Build();

            var client = new HubClient(connection);
            await connection.StartAsync();
            client.InitialPositions = await connection.InvokeAsync<List<BusPosition>>(TrackingHubContract.JoinRoute, routeId);
            return client;
        }

        public async Task<Guid> PostLocationAsync(string busId, DateTimeOffset recordedAt)
        {
            var report = new LocationReport(busId, TestRoutes.RouteId, Latitude, Longitude, 25, 270, recordedAt);
            using var response = await _http.PostAsJsonAsync("/api/locations", report);
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var accepted = await response.Content.ReadFromJsonAsync<LocationAccepted>();
            return accepted!.EventId;
        }

        public async ValueTask DisposeAsync()
        {
            _http.Dispose();
            await _factory.DisposeAsync();
        }
    }

    private sealed class HubClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private readonly BlockingCollection<BusPosition> _received = [];

        public HubClient(HubConnection connection)
        {
            _connection = connection;
            connection.On<BusPosition>(TrackingHubContract.BusMoved, _received.Add);
        }

        public List<BusPosition> InitialPositions { get; set; } = [];

        public IReadOnlyCollection<BusPosition> Received => _received.ToArray();

        /// <summary>The next position pushed to this client, in arrival order.</summary>
        public Task<BusPosition> NextAsync() =>
            Task.Run(() => _received.TryTake(out var position, Timeout)
                ? position
                : throw new TimeoutException("No busMoved event arrived in time."));

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
            _received.Dispose();
        }
    }
}
