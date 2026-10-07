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
	private readonly Coord? _position;

	private UpdateLocationEffect(
		string unitId,
		long journeyId,
		Coord origin,
		Coord destination,
		int startTick,
		TransitPath? path,
		Coord? position)
	{
		_unitId = unitId;
		_journeyId = journeyId;
		_origin = origin;
		_destination = destination;
		_startTick = startTick;
		_path = path;
		_position = position;
	}

	public static UpdateLocationEffect BeginJourney(
		string unitId,
		long journeyId,
		Coord origin,
		Coord destination,
		int startTick,
		TransitPath path) =>
		new(unitId, journeyId, origin, destination, startTick, path, null);

	public static UpdateLocationEffect StopAt(string unitId, Coord position) =>
		new(unitId, 0, default, default, 0, null, position);

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		var state = world.StateOf(_unitId);

		if (_path is not null)
		{
			if (WorkScheduler.HasAssignment(world, _unitId))
			{
				throw new InvalidOperationException(
					$"Fleet '{_unitId}' is not ready to move.");
			}

			runtime.CachedPath = _path;
			state.StartJourney(_journeyId, _origin, _destination, _startTick);
			return [];
		}

		if (_position is not null)
		{
			state.StopAt(_position.Value);
			return [];
		}

		throw new InvalidOperationException("UpdateLocationEffect has no location change.");
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
