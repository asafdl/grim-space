using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.Tests.World.StarSystem.Contact;

[StarSystemTestSuite]
public sealed class HostileContactQueriesTests(StarMapFixture maps)
{
	[Fact]
	public void FindEligibleTargets_RequiresCombatCapableFleetsAndOrdersByDistanceThenId()
	{
		var map = maps.Fresh(42);
		var playerDock = map.DocksByPoiId[map.Blueprint.SupplyPlan.TradeHubPoiId];
		var playerPosition = playerDock.Position + Coord.Forward;
		map.FleetRegistry.Add(Factory.Create(
			new Spawn(
				"player",
				EType.PlayerFleet,
				playerPosition,
				UnitDefaults.SpeedPerTick(EType.PlayerFleet),
				UnitDefaults.EngageRadius(EType.PlayerFleet),
				UnitDefaults.VisionRadius(EType.PlayerFleet),
				[],
				EFaction.Player),
			[BattleUnitType.Fighter]));
		map.FleetRegistry.Add(CreatePirate(
			"pirate-far",
			playerPosition + new Coord(10, 0, 10),
			5));
		map.FleetRegistry.Add(CreatePirate(
			"pirate-near",
			playerPosition + new Coord(2, 0, 2),
			5));
		map.FleetRegistry.Add(CreatePirate(
			"pirate-empty",
			playerPosition + new Coord(1, 0, 1),
			5,
			[]));

		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			"player",
			map: map);
		var observer = map.FleetRegistry.FleetOf("pirate-near");
		var player = map.FleetRegistry.FleetOf("player");
		Assert.Equal(5, observer.State.AggressionRating);
		Assert.Equal(EFaction.Pirates, observer.State.Faction);
		Assert.Equal(EFaction.Player, player.State.Faction);
		Assert.NotEmpty(observer.Members);
		Assert.NotEmpty(player.Members);
		var targets = HostileContactQueries.FindEligibleTargets(
			map,
			"pirate-near",
			orchestrator.RuntimeFor,
			0,
			map.Timeline.Clock.Current);

		Assert.Equal(["player"], targets.Select(target => target.State.Id));
	}

	private static Fleet CreatePirate(
		string id,
		Coord coord,
		int aggressionRating,
		IReadOnlyList<BattleUnitType>? members = null) =>
		Factory.Create(
			new Spawn(
				id,
				EType.PirateFleet,
				coord,
				UnitDefaults.SpeedPerTick(EType.PirateFleet),
				UnitDefaults.EngageRadius(EType.PirateFleet),
				UnitDefaults.VisionRadius(EType.PirateFleet),
				[],
				EFaction.Pirates,
				AggressionRating: aggressionRating),
			members ?? [BattleUnitType.RepurposedMiner]);
}
