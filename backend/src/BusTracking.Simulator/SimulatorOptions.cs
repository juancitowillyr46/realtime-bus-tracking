namespace BusTracking.Simulator;

public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    public string ApiBaseUrl { get; set; } = "http://localhost:5080";

    /// <summary>Optional. When empty, <c>data/routes</c> is searched upwards from the executable.</summary>
    public string? RoutesDirectory { get; set; }

    /// <summary>Real seconds between two reports of the same bus.</summary>
    public int ReportIntervalSeconds { get; set; } = 5;

    /// <summary>Speeds up the simulated clock: 2 means each bus covers twice the distance per report.</summary>
    public double TimeScale { get; set; } = 1;

    public List<SimulatedBusOptions> Buses { get; set; } = [];
}

public sealed class SimulatedBusOptions
{
    public string BusId { get; set; } = string.Empty;

    public string RouteId { get; set; } = string.Empty;

    public double SpeedKmh { get; set; } = 25;

    public double StartOffsetMeters { get; set; }
}
