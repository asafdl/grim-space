using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Pathfinding;

public sealed class MoveActionTests
{
	[Fact]
	public void Commit_RecordsMoveInTimelineAndStartsJourney()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var destinationDockId = unit.State.NextChoreDockId();
		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var destination = map.DocksById[destinationDockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, unit.Runtime);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		Assert.Equal(EPhase.InTransit, unit.State.Phase);
		Assert.NotEqual(0, unit.State.Journey.JourneyId);
		Assert.Equal(origin, unit.State.Journey.Origin);
		Assert.Equal(destination, unit.State.Journey.Destination);
		Assert.Equal(map.Timeline.Clock.Current, unit.State.Journey.StartTick);
		Assert.Same(path, unit.Runtime.CachedPath);
		Assert.Contains(
			engine.History().OfType<MoveAction>(),
			action => action.UnitId == unit.State.Id && action.Path == path);
	}

	[Fact]
	public void CommittedPosition_InterpolatesAcrossElapsedTicks()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var origin = new Coord(0, 0, 0);
		var destination = new Coord(100, 0, 0);
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var speed = unit.State.SpeedPerTick;

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, unit.Runtime);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		var duration = path.DurationTicks(speed);
		for (var tick = 0; tick < duration; tick++)
		{
			engine.AdvanceTick();
			var (position, _) = unit.State.CommittedPosition(map, path, 0f);
			var expected = path.SampleAtElapsed(tick + 1, speed).Position;
			Assert.Equal(expected, position);
		}
	}

	[Fact]
	public void CompleteMoveAction_ArrivesAtScheduledTick()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var destinationDockId = unit.State.NextChoreDockId();
		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var destination = map.DocksById[destinationDockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);
		var duration = path.DurationTicks(unit.State.SpeedPerTick);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, unit.Runtime);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		for (var tick = 0; tick < duration - 1; tick++)
		{
			engine.AdvanceTick();
			Assert.Equal(EPhase.InTransit, unit.State.Phase);
		}

		engine.AdvanceTick();

		Assert.Equal(EPhase.Working, unit.State.Phase);
		Assert.Equal(destinationDockId, unit.State.DockedAtDockId);
	}

	[Fact]
	public void CompleteMoveAction_InfersDockArrivalFromDestinationCoordinate()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var destinationDockId = unit.State.NextChoreDockId();
		var destination = map.DocksById[destinationDockId].Position;
		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, unit.Runtime);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		var duration = path.DurationTicks(unit.State.SpeedPerTick);
		for (var tick = 0; tick < duration; tick++)
			engine.AdvanceTick();

		Assert.Equal(destinationDockId, unit.State.DockedAtDockId);
		Assert.True(map.DocksByPosition.ContainsKey(destination));
	}

	[Fact]
	public void Repath_CancelsPendingCompletionAndSchedulesNewJourney()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var firstDestination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var firstPath = TransitPath.FromPoints([origin, firstDestination], [1.0, 1.0]);
		var secondDestination = new Coord(firstDestination.X + 50, 0, firstDestination.Z);
		var secondPath = TransitPath.FromPoints([origin, secondDestination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, unit.Runtime);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, firstDestination, firstPath));
		var firstJourneyId = unit.State.Journey.JourneyId;
		var firstCompletionTick = unit.Runtime.PendingCompletionTick;

		engine.AdvanceTick();
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, secondDestination, secondPath));

		Assert.NotEqual(firstJourneyId, unit.State.Journey.JourneyId);
		Assert.Equal(secondDestination, unit.State.Journey.Destination);
		Assert.NotEqual(firstCompletionTick, unit.Runtime.PendingCompletionTick);
		Assert.NotEqual(firstJourneyId, ((CompleteMoveAction)unit.Runtime.PendingCompletion!).JourneyId);
	}

	[Fact]
	public void StaleCompleteMoveAction_IsNoOp()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var destination = map.DocksById[unit.State.NextChoreDockId()].Position;
		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.Register(unit.State.Id, unit.Runtime);
		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));
		var staleJourneyId = unit.State.Journey.JourneyId;

		engine.AdvanceTick();
		engine.Commit(new MoveAction(unit.State.Id, unit.State.Id, destination, path));

		engine.Commit(new CompleteMoveAction(unit.State.Id, unit.State.Id, staleJourneyId));

		Assert.Equal(EPhase.InTransit, unit.State.Phase);
	}
}
