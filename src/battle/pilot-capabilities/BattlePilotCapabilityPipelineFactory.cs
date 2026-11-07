using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.PilotCapabilities;

public static class BattlePilotCapabilityPipelineFactory
{
	public static bool Supports(EPilotCapabilityId capabilityId) =>
		capabilityId is EPilotCapabilityId.ShieldBoost
			or EPilotCapabilityId.OverchargeEngines
			or EPilotCapabilityId.HeatVents
			or EPilotCapabilityId.AdjacentDamageBonus
			or EPilotCapabilityId.LightningShieldDamageBonus
			or EPilotCapabilityId.ScrapDroneSwarmDamageBonus
			or EPilotCapabilityId.ShieldBrace
			or EPilotCapabilityId.StationaryDamageDeflection
			or EPilotCapabilityId.ShieldDynamo
			or EPilotCapabilityId.MarkWeakspot
			or EPilotCapabilityId.ThirstForScrap
			or EPilotCapabilityId.YouLittleRat
			or EPilotCapabilityId.NothingButMetal
			or EPilotCapabilityId.SpawnedUnitDamageBonus
			or EPilotCapabilityId.InTheFray
			or EPilotCapabilityId.ReactiveShields
			or EPilotCapabilityId.ReinforcedHull
			or EPilotCapabilityId.HealingSkillRangeBonus
			or EPilotCapabilityId.OneWithTheVoid
			or EPilotCapabilityId.InitiativePenalty
			or EPilotCapabilityId.HeatGainReduction
			or EPilotCapabilityId.HeatClearBonus
			or EPilotCapabilityId.OutgoingDamageBonus
			or EPilotCapabilityId.IncreasedHullDamageTaken
			or EPilotCapabilityId.SpawnedAllyDamageBonus
			or EPilotCapabilityId.DamageResistance
			or EPilotCapabilityId.BaseHullPenalty;

	public static ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Build(
		IReadOnlySet<EPilotCapabilityId> capabilities)
	{
		ArgumentNullException.ThrowIfNull(capabilities);
		return new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>([]);
	}
}
