using GrimSpace.Core.Actions;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Effects;

public sealed class DamageEffect(string targetUnitId, int damage) : IEffect<BattleSlices>
{
	public void Apply(DamageContext damageContext) => damageContext.ApplyTo(targetUnitId, damage);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Damage);
}
