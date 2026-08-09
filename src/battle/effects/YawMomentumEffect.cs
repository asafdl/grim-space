using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public sealed class YawMomentumEffect(int momDelta) : IEffect<BattleWorld, ActorRuntime>
{
	private int _previousMomentum;
	private int _previousMomentumPaid;

	public IReadOnlyList<IRecord> Apply(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		_previousMomentum = actor.MomentumLevel;
		_previousMomentumPaid = runtime.MomentumPaid;

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
		return [];
	}

	public void Undo(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		actor.MomentumLevel = _previousMomentum;
		runtime.MomentumPaid = _previousMomentumPaid;
	}
}
