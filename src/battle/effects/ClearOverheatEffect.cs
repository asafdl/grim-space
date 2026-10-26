using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class ClearOverheatEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousHeat;
	private bool _previousOverheat;
	private bool _applied;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousHeat = actor.Heat;
		_previousOverheat = actor.Overheat;
		_applied = false;

		if (!actor.HeatProfile.Participates)
			return [];

		actor.Overheat = false;
		actor.Heat = 0;
		_applied = true;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		var actor = world.StateOf(actorId);
		actor.Heat = _previousHeat;
		actor.Overheat = _previousOverheat;
	}
}
