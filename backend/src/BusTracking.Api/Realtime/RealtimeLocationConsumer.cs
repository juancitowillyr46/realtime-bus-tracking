using BusTracking.Contracts;
using BusTracking.Core;
using BusTracking.Core.Routes;
using BusTracking.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace BusTracking.Api.Realtime;

/// <summary>
/// Reads every <see cref="LocationUpdated"/> from its own queue (<c>realtime.location</c>),
/// adds the next stop and pushes it to the app over SignalR. Positions older than the
/// bus's latest one are acknowledged and dropped, so the map never moves a bus backwards.
/// </summary>
public sealed class RealtimeLocationConsumer : RabbitMqConsumer<LocationUpdated>
{
    private readonly RouteCatalog _routes;
    private readonly BusPositionTracker _tracker;
    private readonly IHubContext<TrackingHub> _hub;

    public RealtimeLocationConsumer(
        RabbitMqConnectionProvider connectionProvider,
        RouteCatalog routes,
        BusPositionTracker tracker,
        IHubContext<TrackingHub> hub,
        ILogger<RealtimeLocationConsumer> logger)
        : base(connectionProvider, logger)
    {
        _routes = routes;
        _tracker = tracker;
        _hub = hub;
    }

    protected override string QueueName => TrackingTopology.RealtimeLocationQueue;

    protected override string BindingPattern => TrackingTopology.AllLocationUpdatesPattern;

    protected override async Task HandleAsync(LocationUpdated message, CancellationToken cancellationToken)
    {
        var nextStop = _routes.Find(message.RouteId)?.NextStop(new GeoPoint(message.Latitude, message.Longitude));
        var position = new BusPosition(
            message.EventId,
            message.BusId,
            message.RouteId,
            message.Latitude,
            message.Longitude,
            message.SpeedKmh,
            message.Heading,
            message.RecordedAt,
            nextStop?.StopId,
            nextStop?.Name);

        if (!_tracker.TryUpdate(position))
        {
            Logger.LogInformation(
                "Dropped stale location {EventId} for bus {BusId} recorded at {RecordedAt}",
                message.EventId,
                message.BusId,
                message.RecordedAt);
            return;
        }

        await _hub.Clients
            .Group(TrackingHubContract.RouteGroup(message.RouteId))
            .SendAsync(TrackingHubContract.BusMoved, position, cancellationToken);
    }
}
