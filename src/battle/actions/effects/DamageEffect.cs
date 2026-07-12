using GrimSpace.Battle.Actions.Contexts;

namespace GrimSpace.Battle.Actions.Effects;

public sealed class DamageEffect(string targetUnitId, int damage) : IStateEffect
{
	public void Apply(DamageContext damageContext) => damageContext.ApplyTo(targetUnitId, damage);

	void IStateEffect.Apply(ActionSlices slices) => Apply(slices.Damage);
}
