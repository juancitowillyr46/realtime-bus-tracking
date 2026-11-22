using System.Globalization;
using System.Net.Http.Json;
using BusTracking.Contracts;
using BusTracking.Core.Routes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusTracking.Simulator;

/// <summary>
/// Moves every configured bus along its route and sends its position to the ingest API,
/// exactly as a phone in driver mode will do in phase 3.
/// </summary>
public sealed class SimulatorWorker : BackgroundService
{
    public const string ApiClientName = "ingest-api";

    private readonly SimulatorOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _clock;
    private readonly ILogger<SimulatorWorker> _logger;

    public SimulatorWorker(
        IOptions<SimulatorOptions> options,
        IHttpClientFactory httpClientFactory,
        TimeProvider clock,
        ILogger<SimulatorWorker> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.ReportIntervalSeconds <= 0 || _options.TimeScale <= 0)
        {
            throw new InvalidOperationException("Simulator:ReportIntervalSeconds and Simulator:TimeScale must be greater than zero.");
        }

        var buses = CreateBuses();
        var http = _httpClientFactory.CreateClient(ApiClientName);
        var interval = TimeSpan.FromSeconds(_options.ReportIntervalSeconds);
        var simulatedStep = interval * _options.TimeScale;

        _logger.LogInformation(
            "Simulating {BusCount} buses, reporting every {Interval} s (time scale x{TimeScale})",
            buses.Count,
            _options.ReportIntervalSeconds,
            _options.TimeScale);

        using var timer = new PeriodicTimer(interval, _clock);

        // Report the starting positions right away, then move and report on every tick.
        await Task.WhenAll(buses.Select(bus => SendAsync(http, bus, stoppingToken)));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var bus in buses)
            {
                bus.Advance(simulatedStep, bus.NextSpeed(Random.Shared));
            }

            await Task.WhenAll(buses.Select(bus => SendAsync(http, bus, stoppingToken)));
        }
    }

    private List<SimulatedBus> CreateBuses()
    {
        var routesDirectory = RoutesDirectoryLocator.Resolve(_options.RoutesDirectory, AppContext.BaseDirectory);
        var routes = RouteLoader.LoadDirectory(routesDirectory).ToDictionary(route => route.RouteId);

        foreach (var route in routes.Values)
        {
            _logger.LogInformation(
                "Loaded route {RouteId} ({RouteName}): {LengthKm:F2} km, {StopCount} stops",
                route.RouteId,
                route.Name,
                route.Path.TotalLengthMeters / 1000,
                route.Stops.Count);
        }

        if (_options.Buses.Count == 0)
        {
            throw new InvalidOperationException("Configure at least one bus in Simulator:Buses.");
        }

        return _options.Buses
            .Select(bus => routes.TryGetValue(bus.RouteId, out var route)
                ? new SimulatedBus(bus.BusId, route, bus.SpeedKmh, bus.StartOffsetMeters)
                : throw new InvalidOperationException(
                    $"Bus '{bus.BusId}' uses route '{bus.RouteId}', which is not in {routesDirectory}."))
            .ToList();
    }

    private async Task SendAsync(HttpClient http, SimulatedBus bus, CancellationToken cancellationToken)
    {
        var position = bus.Position;
        var report = new LocationReport(
            bus.BusId,
            bus.Route.RouteId,
            Math.Round(position.Point.Latitude, 6),
            Math.Round(position.Point.Longitude, 6),
            Math.Round(bus.CurrentSpeedKmh, 1),
            Math.Round(position.HeadingDegrees, 1),
            _clock.GetUtcNow());

        try
        {
            using var response = await http.PostAsJsonAsync("api/locations", report, TrackingJson.Options, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "{BusId} at {DistanceKm} km of {RouteId}: {StatusCode}",
                    bus.BusId,
                    (bus.DistanceMeters / 1000).ToString("F2", CultureInfo.InvariantCulture),
                    bus.Route.RouteId,
                    (int)response.StatusCode);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "{BusId}: the API answered {StatusCode}: {Body}",
                    bus.BusId,
                    (int)response.StatusCode,
                    body);
            }
        }
        catch (HttpRequestException exception)
        {
            // Keep simulating: the API may be restarting.
            _logger.LogWarning("{BusId}: could not reach the API ({Message})", bus.BusId, exception.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("{BusId}: the API did not answer in time", bus.BusId);
        }
    }
}
