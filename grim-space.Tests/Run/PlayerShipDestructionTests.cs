using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Run;
using GrimSpace.Tests;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.Tests.World.StarSystem.Traffic;
using BattleUnitType = GrimSpace.Units.Enums.EType;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PlayerShipDestructionTests
{
	[Fact]
	public void BattleOutcome_RemovesDestroyedPlayerShipAndPilotFromRunState()
	{
		using var run = RunState.CreateNewRun(42);
		var survivorId = run.GetFieldedShipIds()[0];
		var doomedId = TypedIdGenerator.NextId("gunship");
		Assert.True(run.TryEnlistPlayerShip(new ShipSpawnDeclaration(doomedId, BattleUnitType.Gunship)));
		Assert.True(run.PilotRegistry.TryGetForShip(doomedId, out var doomedPilot));

		var playerFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		run.StarSystem.Map.FleetRegistry.Add(pirateFleet);
		foreach (var declaration in pirateFleet.Registrations)
			run.ItemInventory.Register(declaration, pirateFleet.State.Id);
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateFleet.State.Id)
			.Apply(
				run.StarSystem.Map,
				new GrimSpace.World.StarSystem.Runtime.ActorRuntime(),
				RunState.PlayerFleetUnitId);
		var engagementId = run.StarSystem.Map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id;
		run.ActiveBattle = EngagementBattleFactory.Create(
			[playerFleet, pirateFleet],
			run.ItemInventory,
			1,
			engagementId,
			playerFleet.State.Id);

		var outcome = new BattleOutcome(
			engagementId,
			EBattleResult.Win,
			[
				OutcomeTestKit.Handoff(
					survivorId,
					run.ItemInventory.Get(survivorId).Spec.Chassis,
					1),
				OutcomeTestKit.Handoff(doomedId, BattleUnitType.Gunship, 0),
				OutcomeTestKit.Handoff(
					pirateFleet.Members[0].Id,
					OutcomeTestKit.ChassisFromShipId(pirateFleet.Members[0].Id),
					0),
			]);

		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(outcome));

		Assert.False(run.ItemInventory.TryGet(doomedId, out _));
		Assert.False(run.PilotRegistry.TryGet(doomedPilot.Id, out _));
		Assert.True(run.ItemInventory.TryGet(survivorId, out _));
		Assert.Equal([survivorId], run.GetFieldedShipIds());
		var worldFleet = run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		Assert.Equal(run.GetFieldedShipIds(), worldFleet.Members.Select(member => member.Id).ToArray());
	}

	[Fact]
	public void BattleOutcome_StillAppliesHandoffForDestroyedEnemyShip()
	{
		using var run = RunState.CreateNewRun(42);
		var playerShipId = Assert.Single(run.GetFieldedShipIds());
		var pirateFleet = StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new GrimSpace.Math.Grid.Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates);
		run.StarSystem.Map.FleetRegistry.Add(pirateFleet);
		var enemyShipId = pirateFleet.Members[0].Id;
		foreach (var declaration in pirateFleet.Registrations)
			run.ItemInventory.Register(declaration, pirateFleet.State.Id);
		new CommitEngagementEffect(RunState.PlayerFleetUnitId, pirateFleet.State.Id)
			.Apply(
				run.StarSystem.Map,
				new GrimSpace.World.StarSystem.Runtime.ActorRuntime(),
				RunState.PlayerFleetUnitId);
		var engagementId = run.StarSystem.Map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement!.Id;
		run.ActiveBattle = EngagementBattleFactory.Create(
			[run.StarSystem.Map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId), pirateFleet],
			run.ItemInventory,
			1,
			engagementId,
			RunState.PlayerFleetUnitId);

		var outcome = new BattleOutcome(
			engagementId,
			EBattleResult.Win,
			[
				OutcomeTestKit.Handoff(
					playerShipId,
					run.ItemInventory.Get(playerShipId).Spec.Chassis,
					1),
				OutcomeTestKit.Handoff(
					enemyShipId,
					OutcomeTestKit.ChassisFromShipId(enemyShipId),
					0),
			]);

		run.OnCommittedBattleOutcome(new Record<BattleOutcome>(outcome));

		Assert.True(run.ItemInventory.TryGet(enemyShipId, out var enemy));
		Assert.Equal(0, enemy.HullPoints);
	}
}
