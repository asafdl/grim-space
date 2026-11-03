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
			or EPilotCapabilityId.InTheFray
			or EPilotCapabilityId.ReactiveShields
			or EPilotCapabilityId.ReinforcedHull
			or EPilotCapabilityId.HealingSkillRangeBonus
			or EPilotCapabilityId.OneWithTheVoid
			or EPilotCapabilityId.InitiativePenalty;

	public static ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Build(
		IReadOnlySet<EPilotCapabilityId> capabilities)
	{
		ArgumentNullException.ThrowIfNull(capabilities);
		return new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>([]);
	}
}
