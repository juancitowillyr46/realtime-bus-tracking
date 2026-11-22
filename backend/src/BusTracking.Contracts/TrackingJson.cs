using System.Text.Json;

namespace BusTracking.Contracts;

/// <summary>
/// JSON settings for message bodies: camelCase, the same as the HTTP API.
/// </summary>
public static class TrackingJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
