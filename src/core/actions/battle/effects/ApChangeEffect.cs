using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class ApChangeEffect(int delta) : IEffect<BattleSlices>
{
	public void Apply(ApContext ap) => ap.Change(delta);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Ap);
}
