using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class MarkSpinBrakedEffect : IEffect<BattleSlices>
{
	public void Apply(TurnPhaseContext phaseContext)
	{
		phaseContext.SpinBraked = true;
		phaseContext.SpinDiscount = true;
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.PhaseContext);
}
