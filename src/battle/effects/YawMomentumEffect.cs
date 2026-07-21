using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class YawMomentumEffect(int momDelta) : IEffect<BattleSlices>
{
	public void Apply(State actor, TurnState turnState)
	{
		if (momDelta > 0)
		{
			var loss = System.Math.Min(momDelta, actor.MomentumLevel);
			actor.MomentumLevel -= loss;
			turnState.AddMomentumPaid(loss);
		}
		else if (momDelta < 0)
		{
			var refund = turnState.RefundMomentum(-momDelta);
			actor.MomentumLevel = System.Math.Min(
				actor.MomentumLevel + refund,
				MomentumConfig.MaxLevel);
		}
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Ap.Player, slices.TurnState);
}
