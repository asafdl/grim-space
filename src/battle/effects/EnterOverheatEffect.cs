using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class EnterOverheatEffect : IEffect<BattleWorld, ActorRuntime>
{
	private bool _previousOverdrive;
	private bool _previousOverheat;
	private bool _applied;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousOverdrive = actor.Overdrive;
		_previousOverheat = actor.Overheat;
		_applied = false;

		if (!actor.HeatProfile.Participates)
			return [];

		actor.Overdrive = false;
		actor.Overheat = true;
		_applied = true;
		return
		[
			new Record<HeatChangedFacts>(new HeatChangedFacts(
				actorId,
				actor.Heat,
				actor.Overdrive,
				actor.Overheat)),
		];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			return;

		var actor = world.StateOf(actorId);
		actor.Overdrive = _previousOverdrive;
		actor.Overheat = _previousOverheat;
	}
}
