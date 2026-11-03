using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class SetMarkWeakspotCooldownEffect(int remaining) : IEffect<BattleWorld, ActorRuntime>
{
	private int _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previous = actor.MarkWeakspotCooldownRemaining;
		actor.MarkWeakspotCooldownRemaining = remaining;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).MarkWeakspotCooldownRemaining = _previous;
}
