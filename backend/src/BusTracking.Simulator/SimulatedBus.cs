using BusTracking.Core;

namespace BusTracking.Simulator;

/// <summary>
/// A virtual bus that moves along its route at a slightly variable speed and starts over at the end.
/// Stops at bus stops arrive in phase 4.
/// </summary>
public sealed class SimulatedBus
{
    public const double MinimumSpeedKmh = 5;
    public const double SpeedVariationKmh = 5;

    public SimulatedBus(string busId, RouteDefinition route, double baseSpeedKmh, double startOffsetMeters)
    {
        BusId = busId;
        Route = route;
        BaseSpeedKmh = baseSpeedKmh;
        CurrentSpeedKmh = baseSpeedKmh;
        DistanceMeters = Wrap(startOffsetMeters);
    }

    public string BusId { get; }

    public RouteDefinition Route { get; }

    public double BaseSpeedKmh { get; }

    public double CurrentSpeedKmh { get; private set; }

    public double DistanceMeters { get; private set; }

    public RoutePosition Position => Route.Path.PositionAt(DistanceMeters);

    public void Advance(TimeSpan elapsed, double speedKmh)
    {
        CurrentSpeedKmh = speedKmh;
        DistanceMeters = Wrap(DistanceMeters + (speedKmh / 3.6 * elapsed.TotalSeconds));
    }

    /// <summary>
    /// The base speed plus or minus <see cref="SpeedVariationKmh"/>, never below <see cref="MinimumSpeedKmh"/>.
    /// </summary>
    public double NextSpeed(Random random) =>
        Math.Max(MinimumSpeedKmh, BaseSpeedKmh + (((random.NextDouble() * 2) - 1) * SpeedVariationKmh));

    private double Wrap(double distanceMeters)
    {
        var total = Route.Path.TotalLengthMeters;
        var wrapped = distanceMeters % total;
        return wrapped < 0 ? wrapped + total : wrapped;
    }
}
