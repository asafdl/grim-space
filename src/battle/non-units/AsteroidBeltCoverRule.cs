using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.NonUnits;

public sealed class AsteroidBeltCoverRule : DamageRule
{
	protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
		context.World.AsteroidBeltAt(damage.Target(context.World).Position) is not null;

	protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.ReduceHit(100 - context.World.AsteroidBeltAt(damage.Target(context.World).Position)!.DamageResistancePercent);
}
