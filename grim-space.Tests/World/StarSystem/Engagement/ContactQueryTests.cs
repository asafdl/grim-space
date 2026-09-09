using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

public sealed class ContactQueryTests(DevStarMapFixture maps)
{
	[Fact]
	public void CommittedPositionOf_DockedUnit_ReturnsDockPosition()
	{
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(maps, 42);
		var unit = orchestrator.Map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var dockPosition = orchestrator.Map.DocksById[unit.State.DockedAtDockId].Position;

		Assert.Equal(dockPosition, orchestrator.CommittedPositionOf(unit.State.Id));
	}

	[Fact]
	public void CommittedPositionOf_IdleUnit_ReturnsIdleCoord()
	{
		var map = maps.Fresh(42);
		var pirateId = AddPirate(map, new Coord(30, 0, 40));
		var orchestrator = StarSystemTestHarness.CreateOrchestrator(map);

		Assert.Equal(new Coord(30, 0, 40), orchestrator.CommittedPositionOf(pirateId));
	}

	[Fact]
	public void CommittedPositionOf_InTransit_RebuildsMissingCache()
	{
		var orchestrator = CreatePlayerOrchestrator(42);
		var playerId = RunState.PlayerFleetUnitId;
		var origin = orchestrator.CommittedPositionOf(playerId);
		var destination = new Coord(origin.X + 50, 0, origin.Z + 50);
		orchestrator.PlayerAgent!.TryQueueMove(destination);
		orchestrator.AdvanceTick();
		orchestrator.RuntimeFor(playerId).CachedPath = null;

		var position = orchestrator.CommittedPositionOf(playerId, 0.5f);

		Assert.NotNull(orchestrator.RuntimeFor(playerId).CachedPath);
		Assert.Equal(EPhase.InTransit, orchestrator.Map.StateOf(playerId).Phase);
		Assert.NotEqual(orchestrator.CommittedPositionOf(playerId, 0f), position);
	}

	[Fact]
	public void IsHunterInContactWithTarget_OutsideHunterEngageRadius_ReturnsFalse()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var pirateId = AddPirate(map, new Coord(100, 0, 100));
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, 
			RunState.PlayerFleetUnitId,
			42,
			new StraightLinePathfinder(),
			map);

		Assert.False(EngagementQueries.IsHunterInEngageRange(
			orchestrator.Map,
			RunState.PlayerFleetUnitId,
			pirateId,
			id => orchestrator.CommittedPositionOf(id)));
	}

	[Fact]
	public void IsHunterInContactWithTarget_WithinHunterEngageRadius_ReturnsTrue()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		player.State.Phase = EPhase.Docked;
		player.State.DockedAtDockId = "";
		player.State.IdleCoord = new Coord(0, 0, 0);
		var pirateId = AddPirate(map, new Coord(6, 0, 0));
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, 
			RunState.PlayerFleetUnitId,
			42,
			new StraightLinePathfinder(),
			map);

		Assert.True(EngagementQueries.IsHunterInEngageRange(
			orchestrator.Map,
			RunState.PlayerFleetUnitId,
			pirateId,
			id => orchestrator.CommittedPositionOf(id)));
	}

	[Fact]
	public void IsHunterInContactWithTarget_BeyondHunterEngageRadius_ReturnsFalse()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.UnitRegistry.UnitOf(RunState.PlayerFleetUnitId);
		player.State.Phase = EPhase.Docked;
		player.State.DockedAtDockId = "";
		player.State.IdleCoord = new Coord(0, 0, 0);
		var pirateId = AddPirate(map, new Coord(7, 0, 0));
		var orchestrator = StarSystemTestHarness.CreatePlayerOrchestrator(maps, 
			RunState.PlayerFleetUnitId,
			42,
			new StraightLinePathfinder(),
			map);

		Assert.False(EngagementQueries.IsHunterInEngageRange(
			orchestrator.Map,
			RunState.PlayerFleetUnitId,
			pirateId,
			id => orchestrator.CommittedPositionOf(id)));
	}

	private StarSystemOrchestrator CreatePlayerOrchestrator(int seed)
	{
		var map = maps.Fresh(seed);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		return StarSystemTestHarness.CreatePlayerOrchestrator(maps, 
			RunState.PlayerFleetUnitId,
			seed,
			map: map);
	}

	private static string AddPirate(StarMap map, Coord coord)
	{
		var id = "pirate-contact";
		map.UnitRegistry.Add(Factory.CreatePirateFleet(
			id,
			coord,
			GrimSpace.World.Factions.EFaction.Pirates,
			new CombatProfile(EDangerLevel.VeryLow, 1)));
		return id;
	}

	private sealed class StraightLinePathfinder : IPathfinder
	{
		public PathfindingResult FindPath(Coord origin, Coord destination) =>
			new PathfindingResult.Found(
				TransitPath.FromPoints([origin, destination], [1.0, 1.0]));
	}
}
