using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class MomentumDecayEffect : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId) =>
		world.StateOf(actorId).MomentumLevel =
			System.Math.Max(world.StateOf(actorId).MomentumLevel - 1, 0);
}
