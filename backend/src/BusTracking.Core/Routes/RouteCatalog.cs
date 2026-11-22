namespace BusTracking.Core.Routes;

/// <summary>
/// The routes loaded from <c>data/routes</c>, looked up by id. Loaded once per process.
/// </summary>
public sealed class RouteCatalog
{
    private readonly Dictionary<string, RouteDefinition> _routes;

    public RouteCatalog(IEnumerable<RouteDefinition> routes)
    {
        _routes = routes.ToDictionary(route => route.RouteId, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<RouteDefinition> All => _routes.Values;

    public RouteDefinition? Find(string routeId) => _routes.GetValueOrDefault(routeId);
}
