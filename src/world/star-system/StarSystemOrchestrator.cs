using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed class StarSystemOrchestrator
{
	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly IPathfinder _pathfinder;

	private StarSystemOrchestrator(
		Engine<StarMap, ActorRuntime> engine,
		IPathfinder pathfinder,
		string? playerId)
	{
		_engine = engine;
		_pathfinder = pathfinder;
		PlayerId = playerId;
	}

	public StarMap Map => _engine.World;

	public int Tick => _engine.Tick;

	public string? PlayerId { get; }

	public static StarSystemOrchestrator FromBuildResult(StarSystemBuildResult result) =>
		FromBuildResult(result, new CachedPathfinder(new AStarPathfinder(result.Terrain)), playerId: null);

	public static StarSystemOrchestrator FromBuildResult(
		StarSystemBuildResult result,
		string playerId) =>
		FromBuildResult(
			result,
			new CachedPathfinder(new AStarPathfinder(result.Terrain)),
			playerId);

	public static StarSystemOrchestrator FromBuildResult(
		StarSystemBuildResult result,
		IPathfinder pathfinder,
		string? playerId = null)
	{
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		if (result.Map.Timeline.Clock.Current == 0)
			result.Map.Timeline.Clock.Set(1);

		foreach (var unit in result.Map.UnitRegistry.All)
		{
			actorRuntimes.Register(unit.State.Id, unit.Runtime);
			TransitCache.RebuildIfMissing(unit, pathfinder);
			ScheduleSpawnedWorkerIfNeeded(result.Map, unit);
		}

		return new StarSystemOrchestrator(
			new Engine<StarMap, ActorRuntime>(result.Map, actorRuntimes),
			pathfinder,
			playerId);
	}

	public MoveCommandResult OrderMove(Coord destination)
	{
		if (PlayerId is not { } playerId)
			throw new InvalidOperationException("Cannot order move without a player unit.");

		var unit = _engine.World.UnitRegistry.UnitOf(playerId);
		var (origin, _) = unit.State.CommittedPosition(
			_engine.World,
			unit.Runtime.CachedPath,
			0f);
		var result = _pathfinder.FindPath(origin, destination);
		if (result is not PathfindingResult.Found found)
			return new MoveCommandResult.Unreachable();

		_engine.Commit(new MoveAction(playerId, playerId, destination, found.Path));
		return new MoveCommandResult.Committed(found.Path);
	}

	public IReadOnlyList<ITimelineEntry> AdvanceTick()
	{
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
			if (state.ChoreDockIds.Count == 0)
				continue;

			if (!state.IsReadyToDepart)
				continue;

			var destinationDockId = state.NextChoreDockId();
			var origin = _engine.World.DocksById[state.DockedAtDockId].Position;
			var destination = _engine.World.DocksById[destinationDockId].Position;
			var result = _pathfinder.FindPath(origin, destination);
			if (result is not PathfindingResult.Found found)
				continue;

			_engine.Commit(new MoveAction(
				state.Id,
				state.Id,
				destination,
				found.Path));
		}
	}

	private static void ScheduleSpawnedWorkerIfNeeded(StarMap map, Units.Unit unit)
	{
		var state = unit.State;
		if (state.Phase != EPhase.Working || state.SpawnWorkPoiId is not { } poiId)
			return;

		WorkScheduler.ScheduleSpawnedWorker(map, unit, poiId, state.SpawnWorkRemainingTicks);
		state.SpawnWorkPoiId = null;
		state.SpawnWorkRemainingTicks = 0;
	}
}
