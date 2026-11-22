using BusTracking.Contracts;

namespace BusTracking.Core;

/// <summary>
/// Checks a location before it is published. Error keys match the JSON field names;
/// messages are in Spanish because they can reach the app.
/// </summary>
public static class LocationReportValidator
{
    public static readonly TimeSpan MaxFutureSkew = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    public static Dictionary<string, string[]> Validate(LocationReport report, DateTimeOffset now)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(report.BusId))
        {
            errors["busId"] = ["El identificador del bus es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(report.RouteId))
        {
            errors["routeId"] = ["El identificador de la ruta es obligatorio."];
        }

        if (!IsInRange(report.Latitude, -90, 90))
        {
            errors["latitude"] = ["La latitud debe estar entre -90 y 90."];
        }

        if (!IsInRange(report.Longitude, -180, 180))
        {
            errors["longitude"] = ["La longitud debe estar entre -180 y 180."];
        }

        if (!IsInRange(report.SpeedKmh, 0, 150))
        {
            errors["speedKmh"] = ["La velocidad debe estar entre 0 y 150 km/h."];
        }

        if (!IsInRange(report.Heading, 0, 360))
        {
            errors["heading"] = ["El rumbo debe estar entre 0 y 360 grados."];
        }

        if (report.RecordedAt > now + MaxFutureSkew)
        {
            errors["recordedAt"] = ["La fecha de registro no puede estar en el futuro."];
        }
        else if (report.RecordedAt < now - MaxAge)
        {
            errors["recordedAt"] = ["La fecha de registro tiene más de una hora de antigüedad."];
        }

        return errors;
    }

    private static bool IsInRange(double value, double min, double max) =>
        double.IsFinite(value) && value >= min && value <= max;
}
