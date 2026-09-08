using System.Reflection;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class DismissEngagementMoveTests
{
	[Fact]
	public void MoveDuringInteractive_ClosesHudButStaysInteractiveUntilDismissed()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		Assert.Equal(ESimMode.Interactive, orchestrator.SimMode);

		var destination = new Coord(50, 0, 50);
		Assert.IsType<CourseCommandResult.Queued>(orchestrator.PlayerAgent!.TryQueueMove(destination));
		Assert.Null(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).EngagementTargetUnitId);
		Assert.Equal(ESimMode.Interactive, orchestrator.SimMode);

		orchestrator.DismissEngagement();
		Assert.Equal(ESimMode.Running, orchestrator.SimMode);

		var secondDestination = new Coord(60, 0, 60);
		Assert.IsType<CourseCommandResult.Queued>(orchestrator.PlayerAgent!.TryQueueMove(secondDestination));
	}

	[Fact]
	public void DismissEngagement_AfterHuntContact_PlayerCanQueueMove()
	{
		var orchestrator = CreateHuntContactScenario();
		Assert.Equal(ESimMode.Interactive, orchestrator.SimMode);

		orchestrator.DismissEngagement();

		var destination = new Coord(50, 0, 50);
		var result = orchestrator.PlayerAgent!.TryQueueMove(destination);

		Assert.IsType<CourseCommandResult.Queued>(result);
		AssertAgentCanPlan(orchestrator);
	}

	[Fact]
	public void DismissEngagement_PlayerCanQueueMoveAfterward()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		Assert.Equal(ESimMode.Interactive, orchestrator.SimMode);

		orchestrator.DismissEngagement();
		Assert.Equal(ESimMode.Running, orchestrator.SimMode);

		var destination = new Coord(50, 0, 50);
		var result = orchestrator.PlayerAgent!.TryQueueMove(destination);

		Assert.IsType<CourseCommandResult.Queued>(result);
		AssertAgentCanPlan(orchestrator);
	}

	[Fact]
	public void DismissEngagement_UndockedIdlePlayerCanMove()
	{
		var orchestrator = CreateUndockedOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		orchestrator.DismissEngagement();

		var destination = new Coord(50, 0, 50);
		var result = orchestrator.PlayerAgent!.TryQueueMove(destination);

		Assert.IsType<CourseCommandResult.Queued>(result);
	}

	[Fact]
	public void DismissEngagement_AfterInteractiveMove_PlayerCanQueueAnotherMove()
	{
		var orchestrator = CreateOverlappingScenario();
		orchestrator.SetRunning();
		orchestrator.AdvanceTick();
		Assert.Equal(ESimMode.Interactive, orchestrator.SimMode);

		var firstDestination = new Coord(10, 0, 10);
		Assert.IsType<CourseCommandResult.Queued>(orchestrator.PlayerAgent!.TryQueueMove(firstDestination));

		orchestrator.DismissEngagement();
		Assert.Equal(ESimMode.Running, orchestrator.SimMode);

		var secondDestination = new Coord(50, 0, 50);
		var result = orchestrator.PlayerAgent!.TryQueueMove(secondDestination);

		Assert.IsType<CourseCommandResult.Queued>(result);
		AssertAgentCanPlan(orchestrator);
	}

	private static void AssertAgentCanPlan(StarSystemOrchestrator orchestrator)
	{
		var agent = orchestrator.PlayerAgent!;
		Assert.True(agent.IsPlanning);
	}

	private static StarSystemOrchestrator CreateHuntContactScenario()
	{
		var map = StarMap.CreateDevDefault(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var playerDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		var pirateId = "pirate-contact";
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			pirateId,
			new Coord(playerDock.Position.X + 4, 0, playerDock.Position.Z),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(RunState.PlayerFleetUnitId, 42, map: map);
		orchestrator.PlayerAgent!.TryQueueHuntUnit(pirateId);
		orchestrator.AdvanceTick();
		return orchestrator;
	}

	private static StarSystemOrchestrator CreateOverlappingScenario()
	{
		var map = StarMap.CreateDevDefault(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var playerDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		var pirateId = "pirate-contact";
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			pirateId,
			new Coord(playerDock.Position.X + 4, 0, playerDock.Position.Z),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);

		return StarSystemTestHarness.CreatePlayerOrchestrator(RunState.PlayerFleetUnitId, 42, map: map);
	}

	private static StarSystemOrchestrator CreateUndockedOverlappingScenario()
	{
		var map = StarMap.CreateDevDefault(42);
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

		return StarSystemTestHarness.CreatePlayerOrchestrator(RunState.PlayerFleetUnitId, 42, map: map);
	}
}
