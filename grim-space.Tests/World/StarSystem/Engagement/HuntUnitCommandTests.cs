using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class HuntUnitCommandTests
{
	[Fact]
	public void TryQueueHuntUnit_QueuesCourseWithoutMutatingLiveMap()
	{
		var orchestrator = CreateScenario();
		var pirateId = orchestrator.Map.UnitRegistry.All
			.Single(unit => unit.State.Type == EType.PirateFleet)
			.State.Id;

		var result = orchestrator.PlayerAgent!.TryQueueHuntUnit(pirateId);

		Assert.IsType<CourseCommandResult.Queued>(result);
		Assert.NotNull(orchestrator.PlayerAgent.PendingCourse);
		Assert.Equal(
			orchestrator.CommittedPositionOf(pirateId),
			orchestrator.PlayerAgent.PendingCourse!.Destination);
		Assert.Null(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId).EngagementTargetUnitId);
	}

	[Fact]
	public void TryQueueHuntUnit_RejectsPlayerFleetTarget()
	{
		var orchestrator = CreateScenario();
		var result = orchestrator.PlayerAgent!.TryQueueHuntUnit(RunState.PlayerFleetUnitId);
		Assert.IsType<CourseCommandResult.Unreachable>(result);
	}

	private static StarSystemOrchestrator CreateScenario()
	{
		var map = StarMap.CreateDevDefault(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			"pirate-a",
			new Coord(20, 0, 20),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		return StarSystemTestHarness.CreatePlayerOrchestrator(RunState.PlayerFleetUnitId, 42, map: map);
	}
}
