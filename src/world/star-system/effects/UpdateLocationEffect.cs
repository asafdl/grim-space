using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class UpdateLocationEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly string _unitId;
	private readonly long _journeyId;
	private readonly Coord _origin;
	private readonly Coord _destination;
	private readonly int _startTick;
	private readonly TransitPath? _path;
	private readonly string? _dockId;
	private readonly Coord? _coord;

	private UpdateLocationEffect(
		string unitId,
		long journeyId,
		Coord origin,
		Coord destination,
		int startTick,
		TransitPath? path,
		string? dockId,
		Coord? coord)
	{
		_unitId = unitId;
		_journeyId = journeyId;
		_origin = origin;
		_destination = destination;
		_startTick = startTick;
		_path = path;
		_dockId = dockId;
		_coord = coord;
	}

	public static UpdateLocationEffect BeginJourney(
		string unitId,
		long journeyId,
		Coord origin,
		Coord destination,
		int startTick,
		TransitPath path) =>
		new(unitId, journeyId, origin, destination, startTick, path, null, null);

	public static UpdateLocationEffect ArriveAtDock(string unitId, string dockId) =>
		new(unitId, 0, default, default, 0, null, dockId, null);

	public static UpdateLocationEffect ArriveAtCoord(string unitId, Coord coord) =>
		new(unitId, 0, default, default, 0, null, null, coord);

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		var state = world.StateOf(_unitId);

		if (_path is not null)
		{
			var canBeginJourney = state.Phase == EPhase.InTransit
				|| state.IsReadyToDepart
				|| state is { ChoreDockIds.Count: 0, Phase: EPhase.Docked };

			if (!canBeginJourney)
			{
				throw new InvalidOperationException(
					$"Unit '{_unitId}' is not ready to move.");
			}

			if (state.Phase != EPhase.InTransit && state.Phase != EPhase.Docked)
			{
				throw new InvalidOperationException(
					$"Unit '{_unitId}' cannot begin a journey from phase '{state.Phase}'.");
			}

			runtime.CachedPath = _path;
			state.StartJourney(_journeyId, _origin, _destination, _startTick);
			return [];
		}

		if (_dockId is not null)
		{
			state.ArriveAt(_dockId);
			return [];
		}

		if (_coord is not null)
		{
			state.ArriveAtFreeSpace(_coord.Value);
			return [];
		}

		throw new InvalidOperationException("UpdateLocationEffect has no location change.");
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
