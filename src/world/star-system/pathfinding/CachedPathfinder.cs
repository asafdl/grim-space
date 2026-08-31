using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Pathfinding;

public sealed class CachedPathfinder : IPathfinder
{
	private readonly IPathfinder _inner;
	private readonly Dictionary<(Coord Origin, Coord Destination), PathfindingResult> _cache = new();

	public CachedPathfinder(IPathfinder inner) =>
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));

	public PathfindingResult FindPath(Coord origin, Coord destination)
	{
		var key = (origin, destination);
		if (_cache.TryGetValue(key, out var cached))
			return cached;

		var result = _inner.FindPath(origin, destination);
		_cache[key] = result;
		return result;
	}
}
