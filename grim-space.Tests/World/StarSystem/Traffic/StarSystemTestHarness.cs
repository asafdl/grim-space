using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

internal static class StarSystemTestHarness
{
	public static StarSystemOrchestrator CreateOrchestrator(int seed = 0) =>
		CreateOrchestrator(StarMap.CreateDevDefault(seed));

	public static StarSystemOrchestrator CreateOrchestrator(StarMap map) =>
		StarSystemOrchestrator.FromBuildResult(BuildResult(map), new StraightLinePathfinder());

	private static StarSystemBuildResult BuildResult(StarMap map) =>
		new(
			map,
			PathfindingTerrain.Create(
				map.Width,
				map.Height,
				map.RoutesById.Values,
				map.PointsOfInterest,
				map.DocksById.Values));

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}
}
