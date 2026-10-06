using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

[StarSystemTestSuite]
public sealed class ContactMonitorTests(StarMapFixture maps)
{
	[Fact]
	public void Contact_ProducesAwaitingDecision()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();

		Assert.Equal(EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
		Assert.True(orchestrator.Map.WaitingForPlayerInput);
		Assert.False(orchestrator.CanAdvance);
	}

	[Fact]
	public void Contact_DoesNotDuplicateWhileAwaitingDecision()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		Assert.Equal(EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));

		orchestrator.AdvanceTick();

		Assert.Equal(EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
	}

	[Fact]
	public void FleeAction_ClearsHuntIntentAndResumesPlayback()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetStepped();
		orchestrator.Step();
		Assert.False(orchestrator.CanAdvance);

		var pirateId = orchestrator.Map.FleetRegistry.All
			.Single(unit => unit.State.Type == EType.PirateFleet)
			.State.Id;
		orchestrator.PlayerAgent!.TryEnqueue([new FleeAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.WaitingForPlayerInput);
		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
		Assert.Null(EngagementAssertions.Hunting(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
		Assert.Null(EngagementAssertions.HuntedBy(orchestrator.Map.StateOf(pirateId)));
	}

	[Fact]
	public void EnemyContact_PreservesDecisionUntilPlayerActs()
	{
		var orchestrator = CreateOverlappingEnemyScenario();
		orchestrator.SetRunning();

		orchestrator.AdvanceTick();

		Assert.Equal(
			EEngagementPhase.AwaitingDecision,
			EngagementAssertions.Phase(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
		Assert.True(orchestrator.Map.WaitingForPlayerInput);
		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[new EngageAction(RunState.PlayerFleetUnitId)]));
	}

	[Fact]
	public void EnemyContact_FleeBlocksCounterpartyActionsForTenTicks()
	{
		var orchestrator = CreateOverlappingEnemyScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		var pirate = orchestrator.Map.FleetRegistry.All.Single(
			unit => unit.State.Type == EType.PirateFleet);

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue(
			[new FleeAction(RunState.PlayerFleetUnitId)]));

		var cooldownStartTick = orchestrator.Tick;
		Assert.Equal(
			cooldownStartTick + 10,
			orchestrator.RuntimeFor(pirate.State.Id).ActionCooldownUntilTick);
		Assert.Null(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).CurrentEngagement);
		Assert.Null(pirate.State.CurrentEngagement);
		Assert.Equal(EPhase.Docked, pirate.State.Phase);

		for (var tick = 1; tick < 10; tick++)
		{
			orchestrator.AdvanceTick();
			Assert.Equal(EPhase.Docked, pirate.State.Phase);
		}

		orchestrator.AdvanceTick();

		Assert.Equal(cooldownStartTick + 10, orchestrator.Tick);
		Assert.Equal(EPhase.InTransit, pirate.State.Phase);
	}

	private StarSystemOrchestrator CreateOverlappingScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		player.State.Phase = EPhase.Docked;
		player.State.DockedAtDockId = "";
		player.State.IdleCoord = new Coord(0, 0, 0);
		var pirateId = "pirate-contact";
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		new SetTravelTargetEffect(
			RunState.PlayerFleetUnitId,
			TravelTarget.Fleet(pirateId, EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}

	private StarSystemOrchestrator CreateOverlappingEnemyScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		player.State.Phase = EPhase.Docked;
		player.State.DockedAtDockId = "";
		player.State.IdleCoord = new Coord(0, 0, 0);

		const string pirateId = "pirate-contact";
		map.FleetRegistry.Add(Factory.Create(
			new Spawn(
				pirateId,
				EType.PirateFleet,
				"",
				new Coord(4, 0, 0),
				UnitDefaults.SpeedPerTick(EType.PirateFleet),
				UnitDefaults.EngageRadius(EType.PirateFleet),
				UnitDefaults.VisionRadius(EType.PirateFleet),
				[],
				GrimSpace.World.Factions.EFaction.Pirates,
				PatrolRadius: 8),
			[GrimSpace.Units.Enums.EType.RepurposedMiner]));
		new SetEngagementIntentEffect(pirateId, RunState.PlayerFleetUnitId)
			.Apply(map, new ActorRuntime(), pirateId);
		new SetTravelTargetEffect(
			pirateId,
			TravelTarget.Fleet(RunState.PlayerFleetUnitId, EContactIntent.Engagement))
			.Apply(map, new ActorRuntime(), pirateId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(
			maps,
			RunState.PlayerFleetUnitId,
			42,
			map: map);
	}
}
