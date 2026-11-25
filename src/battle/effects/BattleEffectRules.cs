using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Statuses;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Effects;

public static class BattleEffectRules
{
	public static EffectRules<BattleWorld, ActorRuntime> Rules { get; } = new(
		[
			new OverdriveDamageRule(),
			new ThirstForScrap.DamageBonus(),
			new OutgoingDamageBonus.Rule(),
			new SpawnedAllyDamageBuff.Rule(),
			new ScrapDroneSwarmDamageBonus.Rule(),
			new NothingButMetal.Rule(),
			new SpawnedUnitDamageBonus.Rule(),
			new PirateDamageBonus.Rule(),
			new LightningShieldDamageBonus.Rule(),
			new AdjacentDamageBonus.Rule(),
			new InTheFray.Rule(),
			new ReactiveShields.DamageReduction(),
			new YouLittleRat.Rule(),
			new DamageResistance.Rule(),
			new StationaryDamageDeflection.Rule(),
			new ProtectionDroneBuff.Rule(),
			new MarkWeakspotDebuff.Rule(),
			new AsteroidBeltOutgoingRule(),
			new AsteroidBeltCoverRule(),
			new ReinforcedHull.Rule(),
			new IncreasedHullDamageTaken.Rule(),
			new HealBonus.Rule(),
			new HealPenalty.Rule(),
		],
		[
			new ThirstForScrap.KillStackReaction(),
			new ResetCooldownOnKillReaction(),
			new ReactiveShields.HitStackReaction(),
		]);
}
