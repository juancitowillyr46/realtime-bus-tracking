using System.Globalization;
using System.Text.Json;
using BusTracking.Core;

namespace BusTracking.Simulator;

/// <summary>
/// Reads routes drawn in geojson.io: a FeatureCollection with exactly one LineString
/// (properties: routeId, name) and one Point per stop (properties: stopId, name, order).
/// </summary>
public static class RouteLoader
{
    public static IReadOnlyList<RouteDefinition> LoadDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Routes directory '{directory}' does not exist.");
        }

        var files = Directory.GetFiles(directory, "*.geojson").Order(StringComparer.Ordinal).ToArray();
        if (files.Length == 0)
        {
            throw new InvalidDataException($"No .geojson files found in '{directory}'.");
        }

        var routes = files.Select(LoadFile).ToList();

        var duplicate = routes.GroupBy(route => route.RouteId).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Route id '{duplicate.Key}' is defined in more than one file in '{directory}'.");
        }

        return routes;
    }

    public static RouteDefinition LoadFile(string path) => Parse(File.ReadAllText(path), Path.GetFileName(path));

    public static RouteDefinition Parse(string geoJson, string sourceName)
    {
        using var document = JsonDocument.Parse(geoJson);

        if (!document.RootElement.TryGetProperty("features", out var features)
            || features.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"{sourceName}: expected a FeatureCollection with a 'features' array.");
        }

        RoutePath? path = null;
        string? routeId = null;
        string? routeName = null;
        var lineCount = 0;
        var stops = new List<RouteStop>();

        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("geometry", out var geometry) || geometry.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var coordinates = geometry.GetProperty("coordinates");
            switch (geometry.GetProperty("type").GetString())
            {
                case "LineString":
                    {
                        lineCount++;
                        var points = coordinates.EnumerateArray().Select(position => ReadPosition(position, sourceName)).ToList();
                        if (points.Count < 2)
                        {
                            throw new InvalidDataException($"{sourceName}: the LineString needs at least two points.");
                        }

                        path = new RoutePath(points);
                        routeId = GetString(feature, "routeId");
                        routeName = GetString(feature, "name");
                        break;
                    }

                case "Point":
                    {
                        var order = GetInt(feature, "order") ?? stops.Count + 1;
                        var stopId = GetString(feature, "stopId") ?? $"stop-{order:00}";
                        var name = GetString(feature, "name") ?? stopId;
                        stops.Add(new RouteStop(stopId, name, order, ReadPosition(coordinates, sourceName)));
                        break;
                    }
            }
        }

        if (lineCount != 1 || path is null)
        {
            throw new InvalidDataException($"{sourceName}: expected exactly one LineString feature but found {lineCount}.");
        }

        if (string.IsNullOrWhiteSpace(routeId))
        {
            throw new InvalidDataException($"{sourceName}: the LineString feature needs a 'routeId' property.");
        }

        return new RouteDefinition(routeId, routeName ?? routeId, path, stops.OrderBy(stop => stop.Order).ToList());
    }

    // GeoJSON positions are [longitude, latitude], the reverse of how coordinates are usually written.
    private static GeoPoint ReadPosition(JsonElement position, string sourceName)
    {
        if (position.ValueKind != JsonValueKind.Array || position.GetArrayLength() < 2)
        {
            throw new InvalidDataException($"{sourceName}: every position must be [longitude, latitude].");
        }

        return new GeoPoint(Latitude: position[1].GetDouble(), Longitude: position[0].GetDouble());
    }

    private static string? GetString(JsonElement feature, string name)
    {
        if (!TryGetProperty(feature, name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    // geojson.io may store numbers typed in its property table as strings, so accept both.
    private static int? GetInt(JsonElement feature, string name)
    {
        if (!TryGetProperty(feature, name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
        {
            return number;
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement feature, string name, out JsonElement value)
    {
        value = default;
        return feature.TryGetProperty("properties", out var properties)
            && properties.ValueKind == JsonValueKind.Object
            && properties.TryGetProperty(name, out value);
    }
}
