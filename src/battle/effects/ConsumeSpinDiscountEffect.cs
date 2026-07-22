using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class ConsumeSpinDiscountEffect : IEffect<BattleSlices>
{
	public void Apply(TurnPhaseContext phaseContext) => phaseContext.SpinDiscount = false;

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.PhaseContext);
}
