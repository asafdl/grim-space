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
			or EPilotCapabilityId.StationaryDamageDeflection
			or EPilotCapabilityId.AlliedDamageReduction;

	public static ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Build(
		IReadOnlySet<EPilotCapabilityId> capabilities)
	{
		ArgumentNullException.ThrowIfNull(capabilities);
		var modifiers = new List<IActionEffectModifier<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>>();
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

		if (modifiers.Count == 0)
			return ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>.Empty;

		return new ActionEffectPipeline<BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>(
			modifiers);
	}
}
