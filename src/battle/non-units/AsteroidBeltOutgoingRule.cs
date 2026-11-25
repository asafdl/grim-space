using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.NonUnits;

public sealed class AsteroidBeltOutgoingRule : DamageRule
{
	protected override bool Matches(BattleEffectContext context, DamageUnitEffect damage) =>
		context.World.AsteroidBeltAt(damage.Origin) is not null;

	protected override DamageUnitEffect Transform(BattleEffectContext context, DamageUnitEffect damage) =>
		damage.ReduceHit(AsteroidBelt.OutgoingDamageRetainedPercent);
}
