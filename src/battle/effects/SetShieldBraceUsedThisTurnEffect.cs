using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class SetShieldBraceUsedThisTurnEffect(bool value) : IEffect<BattleWorld, ActorRuntime>
{
	private bool _previousValue;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousValue = actor.ShieldBraceUsedThisTurn;
		actor.ShieldBraceUsedThisTurn = value;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).ShieldBraceUsedThisTurn = _previousValue;
}
