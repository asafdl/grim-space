using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class HuntUnitActionTests(DevStarMapFixture maps)
{
	[Fact]
	public void Preview_DoesNotMutateLiveMap()
	{
		var (orchestrator, playerId, pirateId) = CreateScenario();
		var destination = orchestrator.CommittedPositionOf(pirateId);

		Assert.True(orchestrator.PlayerAgent!.TryEnqueue([
			CreateHuntAction(orchestrator, playerId, pirateId, destination)]));

		Assert.Null(orchestrator.Map.StateOf(playerId).EngagementTargetUnitId);
		Assert.Null(orchestrator.Map.StateOf(pirateId).HuntedByUnitId);
		Assert.Equal(EPhase.Docked, orchestrator.Map.StateOf(playerId).Phase);
	}

	[Fact]
	public void Commit_StoresBidirectionalHuntLinkAndStartsJourney()
	{
		var (orchestrator, playerId, pirateId) = CreateScenario();
		var destination = orchestrator.CommittedPositionOf(pirateId);
		orchestrator.PlayerAgent!.TryEnqueue([CreateHuntAction(orchestrator, playerId, pirateId, destination)]);
		orchestrator.AdvanceTick();

		Assert.Equal(pirateId, orchestrator.Map.StateOf(playerId).EngagementTargetUnitId);
		Assert.Equal(EEngagementPhase.Pursuing, orchestrator.Map.StateOf(playerId).EngagementPhase);
		Assert.Equal(playerId, orchestrator.Map.StateOf(pirateId).HuntedByUnitId);
		Assert.Equal(EPhase.InTransit, orchestrator.Map.StateOf(playerId).Phase);
		Assert.Equal(destination, orchestrator.Map.StateOf(playerId).Journey.Destination);
	}

	[Fact]
	public void Commit_ReplaceTarget_UpdatesBothSides()
	{
		var (orchestrator, playerId, firstPirateId) = CreateScenario();
		var secondPirateId = AddPirate(orchestrator.Map, "pirate-b", new Coord(40, 0, 40));
		QueueHunt(orchestrator, playerId, firstPirateId);
		orchestrator.AdvanceTick();
		QueueHunt(orchestrator, playerId, secondPirateId);
		orchestrator.AdvanceTick();

		Assert.Equal(secondPirateId, orchestrator.Map.StateOf(playerId).EngagementTargetUnitId);
		Assert.Null(orchestrator.Map.StateOf(firstPirateId).HuntedByUnitId);
		Assert.Equal(playerId, orchestrator.Map.StateOf(secondPirateId).HuntedByUnitId);
	}

	[Fact]
	public void IsLegal_RejectsSelfTarget()
	{
		var (orchestrator, playerId, _) = CreateScenario();
		var sim = orchestrator.CreateSimulation();
		var destination = orchestrator.CommittedPositionOf(playerId);

		Assert.False(sim.TryEnqueue(CreateHuntAction(orchestrator, playerId, playerId, destination)));
	}

	[Fact]
	public void IsLegal_RejectsNonCombatTarget()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var trafficUnit = map.UnitRegistry.All.First(unit => unit.State.ChoreDockIds.Count > 0);
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, RunState.PlayerFleetUnitId, 42, map: map);
		var sim = orchestrator.CreateSimulation();
		var destination = orchestrator.CommittedPositionOf(trafficUnit.State.Id);

		Assert.False(sim.TryEnqueue(CreateHuntAction(
			orchestrator,
			RunState.PlayerFleetUnitId,
			trafficUnit.State.Id,
			destination)));
	}

	[Fact]
	public void Move_ClearsHuntLink()
	{
		var (orchestrator, playerId, pirateId) = CreateScenario();
		QueueHunt(orchestrator, playerId, pirateId);
		orchestrator.AdvanceTick();

		var destination = new Coord(5, 0, 5);
		orchestrator.PlayerAgent!.TryQueueMove(destination);
		orchestrator.AdvanceTick();

		Assert.Null(orchestrator.Map.StateOf(playerId).EngagementTargetUnitId);
		Assert.Null(orchestrator.Map.StateOf(pirateId).HuntedByUnitId);
	}

	private (StarSystemOrchestrator orchestrator, string playerId, string pirateId) CreateScenario()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, "pirate-a", new Coord(20, 0, 20));
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, 
			RunState.PlayerFleetUnitId,
			42,
			map: map);
		return (orchestrator, RunState.PlayerFleetUnitId, pirateId);
	}

	private static HuntUnitAction CreateHuntAction(
		StarSystemOrchestrator orchestrator,
		string playerId,
		string targetId,
		Coord destination)
	{
		var origin = orchestrator.CommittedPositionOf(playerId);
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		return new HuntUnitAction(playerId, targetId, destination, path);
	}

	private static void QueueHunt(StarSystemOrchestrator orchestrator, string playerId, string targetId)
	{
		var destination = orchestrator.CommittedPositionOf(targetId);
		orchestrator.PlayerAgent!.TryEnqueue([CreateHuntAction(orchestrator, playerId, targetId, destination)]);
	}

	private static string AddPirate(StarMap map, string id, Coord coord)
	{
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			id,
			coord,
			EFaction.Pirates,
			new CombatProfile(EDangerLevel.VeryLow, 1)));
		return id;
	}
}
