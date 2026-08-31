using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem;

public sealed class StarSystemOrchestrator
{
	private readonly Engine<StarMap, EmptyRuntime> _engine;
	private readonly IPathfinder _pathfinder;

	private StarSystemOrchestrator(Engine<StarMap, EmptyRuntime> engine, IPathfinder pathfinder)
	{
		_engine = engine;
		_pathfinder = pathfinder;
	}

	public StarMap Map => _engine.World;

	public int Tick => _engine.Tick;

	public static StarSystemOrchestrator FromBuildResult(StarSystemBuildResult result) =>
		FromBuildResult(result, new CachedPathfinder(new AStarPathfinder(result.Terrain)));

	public static StarSystemOrchestrator FromBuildResult(
		StarSystemBuildResult result,
		IPathfinder pathfinder)
	{
		var actorRuntimes = new ActorRuntimes<EmptyRuntime>();
		actorRuntimes.For(StarSystemActorIds.Traffic);
		return new StarSystemOrchestrator(
			new Engine<StarMap, EmptyRuntime>(result.Map, actorRuntimes),
			pathfinder);
	}

	public static StarSystemOrchestrator FromMap(StarMap map)
	{
		var terrain = PathfindingTerrain.Create(
			map.Width,
			map.Height,
			map.RoutesById.Values,
			map.PointsOfInterest,
			map.DocksById.Values);
		return FromBuildResult(new StarSystemBuildResult(map, terrain));
	}

	public IReadOnlyList<ITimelineEntry> AdvanceTick()
	{
		_engine.Commit(new AdvanceTrafficAction(StarSystemActorIds.Traffic));
		CoordinateDepartures();
		return _engine.AdvanceTick();
	}

	public void AdvanceTicks(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		for (var i = 0; i < count; i++)
			AdvanceTick();
	}

	private void CoordinateDepartures()
	{
		foreach (var unit in _engine.World.UnitRegistry.All)
		{
			var state = unit.State;
			if (!state.IsReadyToDepart)
				continue;

			var destinationDockId = state.NextChoreDockId();
			var origin = _engine.World.DocksById[state.DockedAtDockId].Position;
			var destination = _engine.World.DocksById[destinationDockId].Position;
			var result = _pathfinder.FindPath(origin, destination);
			if (result is not PathfindingResult.Found found)
				continue;

			_engine.Commit(new BeginTransitAction(
				StarSystemActorIds.Traffic,
				state.Id,
				destinationDockId,
				found.Path));
		}
	}
}
