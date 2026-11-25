using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ClearOverheatEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousHeat;
	private bool _previousOverheat;
	private bool _applied;
	private HeatChangeEffect? _stormHeat;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousHeat = actor.Heat;
		_previousOverheat = actor.Overheat;
		_applied = false;
		_stormHeat = null;

		if (!actor.HeatProfile.Participates)
			return [];

		actor.Overheat = false;
		actor.Heat = 0;
		_applied = true;
		List<IRecord> records =
		[
			new Record<HeatChangedFacts>(new HeatChangedFacts(
				actorId,
				actor.Heat,
				actor.Overdrive,
				actor.Overheat)),
		];
		if (world.IonStormAt(actor.Position) is not null)
		{
			_stormHeat = HeatChangeEffect.FillToCapacity();
			records.AddRange(_stormHeat.Apply(world, runtime, actorId));
		}

		return records;
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		_stormHeat?.Undo(world, runtime, actorId);
		var actor = world.StateOf(actorId);
		actor.Heat = _previousHeat;
		actor.Overheat = _previousOverheat;
	}
}
