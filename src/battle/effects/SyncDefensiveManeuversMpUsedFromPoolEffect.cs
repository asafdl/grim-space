using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class SyncDefensiveManeuversMpUsedFromPoolEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousMpUsed;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousMpUsed = actor.DefensiveManeuversMpUsed;
		actor.DefensiveManeuversMpUsed = MpUsedFromPool(actor);
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).DefensiveManeuversMpUsed = _previousMpUsed;

	internal static int MpUsedFromPool(State actor) =>
		System.Math.Max(0, actor.Maneuverability.MaxManeuverPoints - actor.ManeuverPoints);
}
