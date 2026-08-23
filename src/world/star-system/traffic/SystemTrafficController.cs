using System.Collections.ObjectModel;

namespace GrimSpace.World.StarSystem.Traffic;

public sealed class SystemTrafficController
{
	private readonly Dictionary<string, HashSet<string>> _occupantsByRouteId = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _routeByActorId = new(StringComparer.Ordinal);

	public IReadOnlyDictionary<string, IReadOnlyCollection<string>> OccupantsByRouteId =>
		_occupantsByRouteId.ToDictionary(
			pair => pair.Key,
			pair => (IReadOnlyCollection<string>)new ReadOnlyCollection<string>(pair.Value.ToArray()),
			StringComparer.Ordinal);

	public static string RouteId(string dockAId, string dockBId)
	{
		var (a, b) = CanonicalPair(dockAId, dockBId);
		return $"route:{a}:{b}";
	}

	public static bool TryGetRoute(
		IReadOnlyDictionary<string, SpaceRoute> routesById,
		string fromDockId,
		string toDockId,
		out SpaceRoute route,
		out bool towardDockB)
	{
		route = null!;
		towardDockB = false;
		var routeId = RouteId(fromDockId, toDockId);
		if (!routesById.TryGetValue(routeId, out var resolvedRoute))
			return false;

		route = resolvedRoute;
		towardDockB = route.DockAId == fromDockId;
		return true;
	}

	public static string DestinationDock(SpaceRoute route, bool towardDockB) =>
		towardDockB ? route.DockBId : route.DockAId;

	public bool TryRegisterLane(string actorId, string routeId, bool towardDockB)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);
		ArgumentException.ThrowIfNullOrEmpty(routeId);

		if (_routeByActorId.ContainsKey(actorId))
			return false;

		if (!_occupantsByRouteId.TryGetValue(routeId, out var occupants))
		{
			occupants = new HashSet<string>(StringComparer.Ordinal);
			_occupantsByRouteId[routeId] = occupants;
		}

		occupants.Add(actorId);
		_routeByActorId[actorId] = routeId;
		return true;
	}

	public void UnregisterLane(string actorId)
	{
		ArgumentException.ThrowIfNullOrEmpty(actorId);

		if (!_routeByActorId.Remove(actorId, out var routeId))
			return;

		if (!_occupantsByRouteId.TryGetValue(routeId, out var occupants))
			return;

		occupants.Remove(actorId);
		if (occupants.Count == 0)
			_occupantsByRouteId.Remove(routeId);
	}

	public bool VerifyLane(string actorId, string routeId, bool towardDockB) => true;

	public void Validate()
	{
		var seenActors = new HashSet<string>(StringComparer.Ordinal);
		foreach (var (routeId, occupants) in _occupantsByRouteId)
		{
			foreach (var actorId in occupants)
			{
				if (!seenActors.Add(actorId))
				{
					throw new InvalidOperationException(
						$"Actor '{actorId}' is registered on multiple routes.");
				}

				if (_routeByActorId[actorId] != routeId)
				{
					throw new InvalidOperationException(
						$"Actor '{actorId}' route index mismatch for route '{routeId}'.");
				}
			}
		}

		foreach (var (actorId, routeId) in _routeByActorId)
		{
			if (!_occupantsByRouteId.TryGetValue(routeId, out var occupants)
				|| !occupants.Contains(actorId))
			{
				throw new InvalidOperationException(
					$"Actor '{actorId}' is missing from occupants of route '{routeId}'.");
			}
		}
	}

	public SystemTrafficController Fork()
	{
		var clone = new SystemTrafficController();
		foreach (var (routeId, occupants) in _occupantsByRouteId)
			clone._occupantsByRouteId[routeId] = new HashSet<string>(occupants, StringComparer.Ordinal);

		foreach (var (actorId, routeId) in _routeByActorId)
			clone._routeByActorId[actorId] = routeId;

		return clone;
	}

	private static (string A, string B) CanonicalPair(string dockAId, string dockBId) =>
		string.Compare(dockAId, dockBId, StringComparison.Ordinal) <= 0
			? (dockAId, dockBId)
			: (dockBId, dockAId);
}
