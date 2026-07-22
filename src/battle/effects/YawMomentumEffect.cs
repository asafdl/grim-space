using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class YawMomentumEffect(int momDelta) : IEffect<BattleSlices>
{
	public void Apply(State actor, TurnPhaseContext phaseContext)
	{
		if (momDelta > 0)
		{
			var loss = System.Math.Min(momDelta, actor.MomentumLevel);
			actor.MomentumLevel -= loss;
			if (loss > 0)
				phaseContext.MomentumPaid += loss;
		}
		else if (momDelta < 0)
		{
			var requested = -momDelta;
			var refund = System.Math.Min(requested, phaseContext.MomentumPaid);
			phaseContext.MomentumPaid -= refund;
			actor.MomentumLevel = System.Math.Min(
				actor.MomentumLevel + refund,
				MomentumConfig.MaxLevel);
		}
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) =>
		Apply(slices.Ap.Player, slices.PhaseContext);
}
