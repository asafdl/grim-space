using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Pathfinding;

public sealed class BeginTransitActionTests
{
	[Fact]
	public void Commit_RecordsPathInTimelineAndStartsTransit()
	{
		var map = StarMap.CreateDevDefault(42);
		var unit = map.UnitRegistry.All.First(candidate => candidate.State.IsReadyToDepart);
		var destinationDockId = unit.State.NextChoreDockId();
		var origin = map.DocksById[unit.State.DockedAtDockId].Position;
		var destination = map.DocksById[destinationDockId].Position;
		var path = TransitPath.FromPoints([origin, destination], [1.0, 1.0]);

		var actorRuntimes = new ActorRuntimes<EmptyRuntime>();
		actorRuntimes.For(StarSystemActorIds.Traffic);
		var engine = new Engine<StarMap, EmptyRuntime>(map, actorRuntimes);
		engine.Commit(new BeginTransitAction(
			StarSystemActorIds.Traffic,
			unit.State.Id,
			destinationDockId,
			path));

		Assert.Equal(EPhase.InTransit, unit.State.Phase);
		Assert.Same(path, unit.State.Journey.Path);
		Assert.Contains(
			engine.History().OfType<BeginTransitAction>(),
			action => action.UnitId == unit.State.Id && action.Path == path);
	}
}
