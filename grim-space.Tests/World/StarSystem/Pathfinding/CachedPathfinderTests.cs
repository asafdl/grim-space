using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.Tests.World.StarSystem.Pathfinding;

public sealed class CachedPathfinderTests
{
	[Fact]
	public void FindPath_CachesResultsPerDirectionPair()
	{
		var inner = new RecordingPathfinder();
		var cached = new CachedPathfinder(inner);
		var origin = new Coord(1, 0, 1);
		var destination = new Coord(4, 0, 4);

		cached.FindPath(origin, destination);
		cached.FindPath(origin, destination);
		cached.FindPath(destination, origin);

		Assert.Equal(2, inner.CallCount);
	}

	private sealed class RecordingPathfinder : IPathfinder
	{
		public int CallCount { get; private set; }

		public PathfindingResult FindPath(Coord origin, Coord destination)
		{
			CallCount++;
			return new PathfindingResult.Found(
				TransitPath.FromPoints(
					[origin, destination],
					[1.0, 1.0]));
		}
	}
}
