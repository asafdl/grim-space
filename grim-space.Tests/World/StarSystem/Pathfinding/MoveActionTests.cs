using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Pathfinding;

[StarSystemTestSuite]
public sealed class MoveActionTests(StarMapFixture maps)
{
	[Fact]
	public void Commit_RecordsMoveInTimelineAndStartsJourney()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var destinationDockId = unit.State.NextChoreDockId();
		var origin = map.DockAt(unit.State)!.Position;
		var destination = map.DocksById[destinationDockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		var runtime = actorRuntimes.For(unit.State.Id);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		var journey = unit.State.Journey();
		Assert.NotEqual(0, journey.Id);
		Assert.Equal(origin, journey.Origin);
		Assert.Equal(destination, journey.Destination);
		Assert.Equal(map.Timeline.Clock.Current, journey.StartTick);
		Assert.Same(path, runtime.CachedPath);
		Assert.Contains(
			engine.History().OfType<MoveAction>(),
			action => action.UnitId == unit.State.Id && action.Path == path);
	}

	[Fact]
	public void CommittedPosition_InterpolatesAcrossElapsedTicks()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var origin = new Coord(0, 0, 0);
		var destination = new Coord(100, 0, 0);
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var speed = unit.State.SpeedPerTick;

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		var duration = path.DurationTicks(speed);
		for (var tick = 0; tick < duration; tick++)
		{
			engine.AdvanceTick();
			var (position, _) = unit.State.PositionAt(map, path, 0f);
			var expected = path.SampleAtElapsed(tick + 1, speed).Position;
			Assert.Equal(expected, position);
		}
	}

	[Fact]
	public void CompleteMoveAction_ArrivesAtScheduledTick()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var destinationDockId = unit.State.NextChoreDockId();
		var origin = map.DockAt(unit.State)!.Position;
		var destination = map.DocksById[destinationDockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var duration = path.DurationTicks(unit.State.SpeedPerTick);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		for (var tick = 0; tick < duration - 1; tick++)
		{
			engine.AdvanceTick();
			Assert.IsType<FleetTravel.Journey>(unit.State.Travel);
		}

		engine.AdvanceTick();

		Assert.True(WorkScheduler.IsWorking(map, unit.State.Id));
		Assert.Equal(destinationDockId, map.DockAt(unit.State)!.Id);
	}

	[Fact]
	public void CompleteMoveAction_InfersDockArrivalFromDestinationCoordinate()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var destinationDockId = unit.State.NextChoreDockId();
		var destination = map.DocksById[destinationDockId].Position;
		var origin = map.DockAt(unit.State)!.Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		var duration = path.DurationTicks(unit.State.SpeedPerTick);
		for (var tick = 0; tick < duration; tick++)
			engine.AdvanceTick();

		Assert.Equal(destinationDockId, map.DockAt(unit.State)!.Id);
		Assert.True(map.DocksByPosition.ContainsKey(destination));
	}

	[Fact]
	public void Repath_CancelsPendingCompletionAndSchedulesNewJourney()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var firstDestination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var origin = map.DockAt(unit.State)!.Position;
		var firstPath = TransitPath.FromPoints([origin, firstDestination], [1.0, 1.0]);
		var secondDestination = new Coord(firstDestination.X + 50, 0, firstDestination.Z);
		var secondPath = TransitPath.FromPoints([origin, secondDestination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		var runtime = actorRuntimes.For(unit.State.Id);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, firstDestination, firstPath));
		var firstJourneyId = unit.State.Journey().Id;
		var firstCompletionTick = runtime.PendingCompletionTick;

		engine.AdvanceTick();
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, secondDestination, secondPath));

		Assert.NotEqual(firstJourneyId, unit.State.Journey().Id);
		Assert.Equal(secondDestination, unit.State.Journey().Destination);
		Assert.NotEqual(firstCompletionTick, runtime.PendingCompletionTick);
		Assert.NotEqual(firstJourneyId, ((CompleteMoveAction)runtime.PendingCompletion!).JourneyId);
	}

	[Fact]
	public void StaleCompleteMoveAction_IsNoOp()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var destination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var origin = map.DockAt(unit.State)!.Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(unit.State.Id);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));
		var staleJourneyId = unit.State.Journey().Id;

		engine.AdvanceTick();
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		engine.Commit(new CompleteMoveAction(unit.State.Id, unit.State.Id, staleJourneyId));

		Assert.IsType<FleetTravel.Journey>(unit.State.Travel);
	}

	[Fact]
	public void IsLegal_ChoreUnitWaitingForScheduledWork_IsIllegal()
	{
		var map = maps.Fresh(42);
		var unit = map.FleetRegistry.All.First(candidate => candidate.State.HasChoreAtDock(map));
		var dockId = map.DockAt(unit.State)!.Id;
		var destination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var origin = map.DocksById[dockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var move = new MoveAction(unit.State.Id, unit.State.Id, destination, path);

		WorkScheduler.ReserveOnArrival(map, unit.State.Id, dockId);

		var runtime = new ActorRuntimes<ActorRuntime>().For(unit.State.Id);
		Assert.False(MoveDef.Instance.IsLegal(move, map, runtime));
	}

	[Fact]
	public void IsLegal_PlayerFleet_IsNotBlockedByScheduledWorkRule()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var player = map.FleetRegistry.FleetOf(RunState.PlayerFleetUnitId);
		var destination = map.DocksByPoiId[SupplySystemPlan.Copper.StoragePoiId].Position;
		var origin = map.DockAt(player.State)!.Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var move = new MoveAction(player.State.Id, player.State.Id, destination, path);

		map.Timeline.Schedule(
			2,
			new BeginWorkAction(player.State.Id, player.State.Id, "any-poi", map.Timeline.Clock.Current + 2));

		var runtime = new ActorRuntimes<ActorRuntime>().For(player.State.Id);
		Assert.True(MoveDef.Instance.IsLegal(move, map, runtime));
	}
}
