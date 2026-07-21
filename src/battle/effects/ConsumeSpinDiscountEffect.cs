using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class ConsumeSpinDiscountEffect : IEffect<BattleSlices>
{
	public void Apply(TurnState turnState) => turnState.TryConsumeSpinDiscount();

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.TurnState);
}
