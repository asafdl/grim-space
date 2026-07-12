using GrimSpace.Battle.Actions.Contexts;

namespace GrimSpace.Battle.Actions.Effects;

public sealed class ApChangeEffect(int delta) : IStateEffect
{
	public void Apply(ApContext ap) => ap.Change(delta);

	void IStateEffect.Apply(ActionSlices slices) => Apply(slices.Ap);
}
