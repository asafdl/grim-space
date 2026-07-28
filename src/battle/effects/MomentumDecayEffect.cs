using GrimSpace.Battle.World;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MomentumDecayEffect : IEffect<BattleWorld, ActorRuntime>
{
	public void Apply(BattleWorld world, ActorRuntime runtime, string actorId) =>
		world.StateOf(actorId).MomentumLevel =
			System.Math.Max(world.StateOf(actorId).MomentumLevel - 1, 0);
}
