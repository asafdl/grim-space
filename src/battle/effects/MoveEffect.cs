using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public sealed class MoveEffect(Coord destination) : IEffect<BattleWorld, ActorRuntime>
{
	private Coord _previous;
	private string? _cooledPlumeId;
	private HeatChangeEffect? _plumeCooling;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previous = actor.Position;
		_cooledPlumeId = null;
		_plumeCooling = null;
		actor.Position = destination;
		if (_previous == destination)
			return [];

		IRecord moved = new Record<MovedFacts>(new MovedFacts(actorId, _previous, destination));
		var plume = world.CryoPlumeAt(destination);
		if (plume is null || !actor.CryoPlumesCooledThisTurn.Add(plume.Id))
			return [moved];

		_cooledPlumeId = plume.Id;
		_plumeCooling = new HeatChangeEffect(-CryoPlume.HeatCleared);
		return
		[
			moved,
			new Record<CryoPlumeCooledFacts>(new CryoPlumeCooledFacts(actorId, plume.Id)),
			.._plumeCooling.Apply(world, runtime, actorId),
		];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		if (_plumeCooling is not null)
		{
			_plumeCooling.Undo(world, runtime, actorId);
			actor.CryoPlumesCooledThisTurn.Remove(_cooledPlumeId!);
		}

		actor.Position = _previous;
	}
}
