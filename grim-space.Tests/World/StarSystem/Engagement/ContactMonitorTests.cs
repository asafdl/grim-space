using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class ContactMonitorTests(DevStarMapFixture maps)
{
	[Fact]
	public void Contact_ProducesAwaitingDecision()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();

		Assert.Equal(EEngagementPhase.AwaitingDecision,
			orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).EngagementPhase);
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
			orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).EngagementPhase);

		orchestrator.AdvanceTick();

		Assert.Equal(EEngagementPhase.AwaitingDecision,
			orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).EngagementPhase);
	}

	[Fact]
	public void FleeAction_ClearsHuntIntentAndResumesPlayback()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetStepped();
		orchestrator.Step();
		Assert.False(orchestrator.CanAdvance);

		var pirateId = orchestrator.Map.UnitRegistry.All
			.Single(unit => unit.State.Type == EType.PirateFleet)
			.State.Id;
		orchestrator.PlayerAgent!.TryEnqueue([new FleeAction(RunState.PlayerFleetUnitId)]);
		orchestrator.AdvanceClock();

		Assert.False(orchestrator.Map.WaitingForPlayerInput);
		Assert.Equal(ESimMode.Stepped, orchestrator.SimMode);
		Assert.Null(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).EngagementTargetUnitId);
		Assert.Null(orchestrator.Map.StateOf(pirateId).HuntedByUnitId);
	}

	private StarSystemOrchestrator CreateOverlappingScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		player.State.Phase = EPhase.Docked;
		player.State.DockedAtDockId = "";
		player.State.IdleCoord = new Coord(0, 0, 0);
		var pirateId = "pirate-contact";
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			pirateId,
			new Coord(4, 0, 0),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}
}
