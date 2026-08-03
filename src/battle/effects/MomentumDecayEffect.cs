using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MomentumDecayEffect : IEffect<BattleWorld, ActorRuntime>
{
	private int _previous;

	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previous = actor.MomentumLevel;
		actor.MomentumLevel = System.Math.Max(actor.MomentumLevel - 1, 0);
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).MomentumLevel = _previous;
}
