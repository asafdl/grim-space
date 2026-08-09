using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class TorpedoCooldownEffect(int cooldownTurns) : IEffect<BattleWorld, ActorRuntime>
{
	private int _previous;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previous = actor.TorpedoCooldownRemaining;
		actor.TorpedoCooldownRemaining = cooldownTurns;
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).TorpedoCooldownRemaining = _previous;
}
