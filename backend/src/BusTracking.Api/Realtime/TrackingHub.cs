using BusTracking.Contracts;
using BusTracking.Core;
using Microsoft.AspNetCore.SignalR;

namespace BusTracking.Api.Realtime;

/// <summary>
/// The app joins a route's group to receive <see cref="TrackingHubContract.BusMoved"/> for its buses.
/// Positions are pushed by <see cref="RealtimeLocationConsumer"/>; the hub itself only manages groups.
/// </summary>
public sealed class TrackingHub : Hub
{
    private readonly BusPositionTracker _tracker;

    public TrackingHub(BusPositionTracker tracker)
    {
        _tracker = tracker;
    }

    /// <summary>
    /// Subscribes to <paramref name="routeId"/> and returns its buses' current positions,
    /// so the app can draw them right away instead of waiting for the next report.
    /// </summary>
    [HubMethodName(TrackingHubContract.JoinRoute)]
    public async Task<IReadOnlyList<BusPosition>> JoinRoute(string routeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, TrackingHubContract.RouteGroup(routeId));
        return _tracker.GetByRoute(routeId);
    }

    [HubMethodName(TrackingHubContract.LeaveRoute)]
    public Task LeaveRoute(string routeId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, TrackingHubContract.RouteGroup(routeId));
}
