using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MpChangeEffect(int delta) : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousManeuverPoints;
	private int _previousMpUsed;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousManeuverPoints = actor.ManeuverPoints;
		_previousMpUsed = actor.DefensiveManeuversMpUsed;
		actor.ManeuverPoints += delta;
		if (actor.DefensiveManeuversActive)
			actor.DefensiveManeuversMpUsed = SyncDefensiveManeuversMpUsedFromPoolEffect.MpUsedFromPool(actor);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.ManeuverPoints = _previousManeuverPoints;
		actor.DefensiveManeuversMpUsed = _previousMpUsed;
	}
}
