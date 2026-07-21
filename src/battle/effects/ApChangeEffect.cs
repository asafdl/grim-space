using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class ApChangeEffect(int delta) : IEffect<BattleSlices>
{
	public void Apply(ApContext ap) => ap.Change(delta);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Ap);
}
