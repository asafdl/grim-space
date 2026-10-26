using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;
using GrimSpace.Pilots;

namespace GrimSpace.Battle.PilotCapabilities;

public static class BattlePilotCapabilityPipelineFactory
{
	public static bool Supports(EPilotCapabilityId capabilityId) =>
		capabilityId is EPilotCapabilityId.ShieldBoost
			or EPilotCapabilityId.AdjacentDamageBonus
			or EPilotCapabilityId.LightningShieldDamageBonus
			or EPilotCapabilityId.ScrapDroneSwarmDamageBonus
			or EPilotCapabilityId.StationaryDamageDeflection
			or EPilotCapabilityId.AlliedDamageReduction
			or EPilotCapabilityId.InitiativePenalty;

	public static ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Build(
		IReadOnlySet<EPilotCapabilityId> capabilities)
	{
		ArgumentNullException.ThrowIfNull(capabilities);
		var modifiers = new List<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>
		{
			new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
				[BattleActionTags.Attack],
				OverdriveDamageBonusModifier.Instance),
		};
		if (capabilities.Contains(EPilotCapabilityId.AdjacentDamageBonus))
		{
			modifiers.Add(new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
				[BattleActionTags.Attack],
				AdjacentDamageBonusModifier.Instance));
		}
		if (capabilities.Contains(EPilotCapabilityId.LightningShieldDamageBonus))
		{
			modifiers.Add(new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
				[BattleActionTags.Attack, BattleActionTags.Lightning],
				LightningShieldDamageBonusModifier.Instance));
		}
		if (capabilities.Contains(EPilotCapabilityId.ScrapDroneSwarmDamageBonus))
		{
			modifiers.Add(new ActionTagEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
				[BattleActionTags.Attack, BattleActionTags.ScrapDrone],
				ScrapDroneSwarmDamageBonusModifier.Instance));
		}

		return new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			modifiers);
	}
}
