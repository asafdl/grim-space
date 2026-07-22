using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class YawMomentumEffect(int momDelta) : IEffect<BattleBoard, ActorSession>
{
	public void Apply(BattleBoard world, ActorSession runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		if (momDelta > 0)
		{
			var loss = System.Math.Min(momDelta, actor.MomentumLevel);
			actor.MomentumLevel -= loss;
			if (loss > 0)
				runtime.MomentumPaid += loss;
		}
		else if (momDelta < 0)
		{
			var requested = -momDelta;
			var refund = System.Math.Min(requested, runtime.MomentumPaid);
			runtime.MomentumPaid -= refund;
			actor.MomentumLevel = System.Math.Min(
				actor.MomentumLevel + refund,
				MomentumConfig.MaxLevel);
		}
	}
}
