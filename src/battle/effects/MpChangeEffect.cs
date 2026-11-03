using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
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
		_previousManeuverPoints = actor.CurrentManeuverPoints;
		_previousMpUsed = actor.DefensiveManeuversMpUsed;
		actor.CurrentManeuverPoints += delta;
		if (BattleStatuses.HasDefensiveManeuvers(actor))
			actor.DefensiveManeuversMpUsed = SyncDefensiveManeuversMpUsedFromPoolEffect.MpUsedFromPool(actor);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.CurrentManeuverPoints = _previousManeuverPoints;
		actor.DefensiveManeuversMpUsed = _previousMpUsed;
	}
}
