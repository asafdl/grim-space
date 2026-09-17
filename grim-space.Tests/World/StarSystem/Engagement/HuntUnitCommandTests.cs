using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class HuntUnitCommandTests(StarMapFixture maps)
{
	[Fact]
	public void TryQueueHuntUnit_QueuesCourseWithoutMutatingLiveMap()
	{
		var orchestrator = CreateScenario();
		var pirateId = orchestrator.Map.FleetRegistry.All
			.Single(unit => unit.State.Type == EType.PirateFleet)
			.State.Id;

		var result = orchestrator.PlayerAgent!.TryQueueHuntUnit(pirateId);

		Assert.IsType<CourseCommandResult.Queued>(result);
		Assert.NotNull(orchestrator.PlayerAgent.PendingCourse);
		Assert.Equal(
			orchestrator.CommittedPositionOf(pirateId),
			orchestrator.PlayerAgent.PendingCourse!.Destination);
		Assert.Null(EngagementAssertions.Hunting(orchestrator.Map.StateOf(RunState.PlayerFleetUnitId)));
	}

	[Fact]
	public void TryQueueHuntUnit_RejectsPlayerFleetTarget()
	{
		var orchestrator = CreateScenario();
		var result = orchestrator.PlayerAgent!.TryQueueHuntUnit(RunState.PlayerFleetUnitId);
		Assert.IsType<CourseCommandResult.Unreachable>(result);
	}

	private StarSystemOrchestrator CreateScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		map.FleetRegistry.Add(StarSystemTestHarness.CreatePirateFleet(
			"pirate-a",
			new Coord(20, 0, 20),
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
	}
}
