using GrimSpace.Battle.Turn;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class AddYawQuartersEffect(int delta) : IEffect<BattleSlices>
{
	public void Apply(TurnPhaseContext phaseContext) => phaseContext.RawYawQuarters += delta;

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.PhaseContext);
}
