namespace GrimSpace.World.StarSystem.Traffic;

public sealed class RouteCatalog
{
	private readonly IReadOnlyDictionary<string, RouteTemplate> _routesById;
	private readonly Dictionary<(string Origin, string Dest), List<RouteTemplate>> _routesByPair;

	public RouteCatalog(IReadOnlyDictionary<string, RouteTemplate> routesById)
	{
		_routesById = routesById;
		_routesByPair = routesById.Values
			.GroupBy(route => (route.OriginPoiId, route.DestinationPoiId))
			.ToDictionary(group => group.Key, group => group.OrderBy(route => route.Id).ToList());
	}

	public RouteTemplate Select(
		int seed,
		string actorId,
		string originPoiId,
		string destinationPoiId)
	{
		var pair = (originPoiId, destinationPoiId);
		if (!_routesByPair.TryGetValue(pair, out var routes))
			throw new ArgumentException($"No routes for {originPoiId} -> {destinationPoiId}");

		var variant = (TrafficHash.Mix(seed, actorId, originPoiId, destinationPoiId) & int.MaxValue) % routes.Count;
		return routes[variant];
	}

	public RouteAssignment Assign(RouteTemplate route) =>
		new(route.Id, 0);

	public RouteTemplate GetRoute(string routeId) => _routesById[routeId];
}
