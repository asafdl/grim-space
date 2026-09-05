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
		StarSystemOrchestrator.FromMap(map, new StraightLinePathfinder());

	public static StarSystemOrchestrator CreatePlayerOrchestrator(
		string playerFleetUnitId,
		int seed = 0,
		IPathfinder? pathfinder = null)
	{
		var map = StarMap.CreateDevDefault(seed);
		AddPlayerFleet(map, playerFleetUnitId);
		return StarSystemOrchestrator.FromMap(
			map,
			pathfinder ?? new StraightLinePathfinder(),
			playerFleetUnitId);
	}

	internal static void AddPlayerFleet(StarMap map, string playerFleetUnitId)
	{
		var tradeHubDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		map.UnitRegistry.Add(Factory.Create(new Spawn(
			playerFleetUnitId,
			EType.PlayerFleet,
			tradeHubDock.Id,
			default,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			[])));
	}

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}
}
