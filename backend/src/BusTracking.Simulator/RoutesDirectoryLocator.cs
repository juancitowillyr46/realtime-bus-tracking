namespace BusTracking.Simulator;

public static class RoutesDirectoryLocator
{
    /// <summary>
    /// Uses the configured directory when there is one; otherwise walks up from
    /// <paramref name="startDirectory"/> until it finds <c>data/routes</c> (the repository root).
    /// </summary>
    public static string Resolve(string? configuredDirectory, string startDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return Path.GetFullPath(configuredDirectory);
        }

        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "data", "routes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not find 'data/routes' above '{startDirectory}'. Set Simulator:RoutesDirectory.");
    }
}
