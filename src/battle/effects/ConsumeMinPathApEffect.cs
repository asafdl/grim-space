using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class ConsumeMinPathApEffect(int stepApCost) : IEffect<BattleSlices>
{
	public void Apply(TurnPhaseContext phaseContext)
	{
		phaseContext.MinPathApCost = System.Math.Max(0, phaseContext.MinPathApCost - stepApCost);
		if (stepApCost > 0)
			phaseContext.PathApSpent += stepApCost;
	}

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.PhaseContext);
}
