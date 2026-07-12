using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle.Effects;

public sealed class DamageEffect(string targetUnitId, int damage) : IEffect<BattleSlices>
{
	public void Apply(DamageContext damageContext) => damageContext.ApplyTo(targetUnitId, damage);

	void IEffect<BattleSlices>.Apply(BattleSlices slices) => Apply(slices.Damage);
}
