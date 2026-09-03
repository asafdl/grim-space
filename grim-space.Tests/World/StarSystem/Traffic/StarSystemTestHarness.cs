using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

internal static class StarSystemTestHarness
{
	public static StarSystemOrchestrator CreateOrchestrator(int seed = 0) =>
		CreateOrchestrator(StarMap.CreateDevDefault(seed));

	public static StarSystemOrchestrator CreateOrchestrator(StarMap map) =>
		StarSystemOrchestrator.FromBuildResult(BuildResult(map), new StraightLinePathfinder());

	public static StarSystemOrchestrator CreatePlayerOrchestrator(
		string playerFleetUnitId,
		int seed = 0,
		IPathfinder? pathfinder = null)
	{
		var buildResult = StarMap.CreateDevBuildResult(seed);
		AddPlayerFleet(buildResult, playerFleetUnitId);
		return StarSystemOrchestrator.FromBuildResult(
			buildResult,
			pathfinder ?? new StraightLinePathfinder(),
			playerFleetUnitId);
	}

	internal static void AddPlayerFleet(StarSystemBuildResult buildResult, string playerFleetUnitId)
	{
		var map = buildResult.Map;
		var tradeHubDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		map.UnitRegistry.Add(Factory.Create(new Spawn(
			playerFleetUnitId,
			EType.PlayerFleet,
			tradeHubDock.Id,
			default,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			[])));
	}

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
