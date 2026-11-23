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
	private HeatChangeEffect? _stormHeat;
	private RefreshTemporaryShieldPercentOfFaceMaxEffect? _stormShields;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previous = actor.Position;
		_cooledPlumeId = null;
		_plumeCooling = null;
		_stormHeat = null;
		_stormShields = null;
		actor.Position = destination;
		if (_previous == destination)
			return [];

		List<IRecord> records = [new Record<MovedFacts>(new MovedFacts(actorId, _previous, destination))];
		var plume = world.CryoPlumeAt(destination);
		if (plume is not null && actor.CryoPlumesCooledThisTurn.Add(plume.Id))
		{
			_cooledPlumeId = plume.Id;
			_plumeCooling = new HeatChangeEffect(-CryoPlume.HeatCleared);
			records.Add(new Record<CryoPlumeCooledFacts>(new CryoPlumeCooledFacts(actorId, plume.Id)));
			records.AddRange(_plumeCooling.Apply(world, runtime, actorId));
		}

		if (world.IonStormAt(destination) is not null && actor.HeatProfile.Participates)
		{
			_stormShields = new RefreshTemporaryShieldPercentOfFaceMaxEffect(
				IonStorm.ShieldGrantId, IonStorm.ShieldPercentOfFaceMax);
			records.AddRange(_stormShields.Apply(world, runtime, actorId));
			_stormHeat = HeatChangeEffect.FillToCapacity();
			records.AddRange(_stormHeat.Apply(world, runtime, actorId));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_stormHeat?.Undo(world, runtime, actorId);
		_stormShields?.Undo(world, runtime, actorId);
		if (_plumeCooling is not null)
		{
			_plumeCooling.Undo(world, runtime, actorId);
			actor.CryoPlumesCooledThisTurn.Remove(_cooledPlumeId!);
		}

		actor.Position = _previous;
	}
}
