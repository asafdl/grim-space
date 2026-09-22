using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Traffic;

internal static class StarSystemTestHarness
{
	public static StarSystemOrchestrator CreateOrchestrator(StarMapFixture maps, int seed = 0) =>
		CreateOrchestrator(maps.FreshWithBeatAHunt(seed));

	public static StarSystemOrchestrator CreateOrchestrator(StarMap map) =>
		StarSystemOrchestrator.FromMap(map, new StraightLinePathfinder());

	public static StarSystemOrchestrator CreatePlayerOrchestrator(
		StarMapFixture maps,
		string playerFleetUnitId,
		int seed = 0,
		IPathfinder? pathfinder = null,
		StarMap? map = null)
	{
		map ??= maps.FreshWithBeatAHunt(seed);
		if (map.FleetRegistry.All.All(unit => unit.State.Id != playerFleetUnitId))
			AddPlayerFleet(map, playerFleetUnitId);
		return StarSystemOrchestrator.FromMap(
			map,
			pathfinder ?? new StraightLinePathfinder(),
			playerFleetUnitId);
	}

	internal static void AddPlayerFleet(StarMap map, string playerFleetUnitId)
	{
		var tradeHubDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		map.FleetRegistry.Add(Factory.Create(
			new Spawn(
				playerFleetUnitId,
				EType.PlayerFleet,
				tradeHubDock.Id,
				default,
				UnitDefaults.SpeedPerTick(EType.PlayerFleet),
				UnitDefaults.EngageRadius(EType.PlayerFleet),
				UnitDefaults.VisionRadius(EType.PlayerFleet),
				[]),
			[BattleUnitType.Fighter]));
	}

	internal static Fleet CreatePirateFleet(
		string id,
		Coord coord,
		EFaction faction,
		CombatProfile combatProfile) =>
		Factory.Create(
			new Spawn(
				id,
				EType.PirateFleet,
				"",
				coord,
				UnitDefaults.SpeedPerTick(EType.PirateFleet),
				UnitDefaults.EngageRadius(EType.PirateFleet),
				UnitDefaults.VisionRadius(EType.PirateFleet),
				[],
				faction,
				combatProfile),
			[
				BattleUnitType.Patrol,
				BattleUnitType.Patrol,
				BattleUnitType.Patrol,
			]);

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}
}
