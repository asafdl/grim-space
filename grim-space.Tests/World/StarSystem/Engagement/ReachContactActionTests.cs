using GrimSpace.Core.Engine;
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

public sealed class ReachContactActionTests(DevStarMapFixture maps)
{
	[Fact]
	public void Commit_TransitionsPursuingToAwaitingDecision()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, new Coord(4, 0, 0));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());

		engine.Commit([new ReachContactAction(RunState.PlayerFleetUnitId, pirateId)]);

		Assert.Equal(EEngagementPhase.AwaitingDecision, map.StateOf(RunState.PlayerFleetUnitId).EngagementPhase);
		Assert.Equal(EEngagementPhase.None, map.StateOf(pirateId).EngagementPhase);
	}

	[Fact]
	public void IsLegal_RejectsDuplicateWhileAwaitingDecision()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, new Coord(4, 0, 0));
		new SetEngagementIntentEffect(RunState.PlayerFleetUnitId, pirateId)
			.Apply(map, new ActorRuntime(), RunState.PlayerFleetUnitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, new ActorRuntimes<ActorRuntime>());
		engine.Commit([new ReachContactAction(RunState.PlayerFleetUnitId, pirateId)]);
		var sim = engine.CreateSimulation();

		Assert.False(sim.TryEnqueue(new ReachContactAction(RunState.PlayerFleetUnitId, pirateId)));
	}

	private static string AddPirate(StarMap map, Coord coord)
	{
		var id = "pirate-a";
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			id,
			coord,
			GrimSpace.World.Factions.EFaction.Pirates,
			new GrimSpace.World.StarSystem.Encounter.CombatProfile(
				GrimSpace.World.StarSystem.Encounter.EDangerLevel.VeryLow,
				1)));
		return id;
	}
}
