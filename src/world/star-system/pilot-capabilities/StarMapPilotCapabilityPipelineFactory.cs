using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Runtime;
namespace GrimSpace.World.StarSystem.PilotCapabilities;

public static class StarMapPilotCapabilityPipelineFactory
{
	public static bool Supports(EPilotCapabilityId capabilityId) =>
		capabilityId is EPilotCapabilityId.MinorScrapRewardBonus
			or EPilotCapabilityId.ScrapRewardBonus
			or EPilotCapabilityId.ShipUpgradeDiscount
			or EPilotCapabilityId.OptimalityContractCreditBonus;

	public static ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Build(
		IReadOnlyList<PilotCapabilityGrant> strategicGrants)
	{
		ArgumentNullException.ThrowIfNull(strategicGrants);
		var modifiers = new List<IActionEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>>();
		var shipUpgradeDiscountStacks = strategicGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.ShipUpgradeDiscount);
		var scrapRewardBonusPercent =
			strategicGrants.Count(grant =>
				grant.CapabilityId == EPilotCapabilityId.MinorScrapRewardBonus)
			* ScrapRewardBonusModifier.MinorBonusPercent
			+ strategicGrants.Count(grant =>
				grant.CapabilityId == EPilotCapabilityId.ScrapRewardBonus)
			* PilotSkillPercents.DronesYearnForMines.ScrapRewardBonusPercent;
		var optimalityContractCreditBonusStacks = strategicGrants.Count(grant =>
			grant.CapabilityId == EPilotCapabilityId.OptimalityContractCreditBonus);
		if (shipUpgradeDiscountStacks > 0)
		{
			modifiers.Add(new ActionTagEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>(
				[StarMapActionTags.Fetch, StarMapActionTags.Merchant, StarMapActionTags.Catalog],
				new ShipUpgradeDiscountModifier(shipUpgradeDiscountStacks)));
		}
		if (scrapRewardBonusPercent > 0)
		{
			modifiers.Add(new ActionTagEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>(
				[StarMapActionTags.Reward, StarMapActionTags.Salvage],
				new ScrapRewardBonusModifier(scrapRewardBonusPercent)));
		}
		if (optimalityContractCreditBonusStacks > 0)
		{
			modifiers.Add(new ActionTagEffectModifier<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>(
				[
					StarMapActionTags.Reward,
					StarMapActionTags.Contract,
					StarMapActionTags.TheOptimality,
				],
				new OptimalityContractCreditBonusModifier(optimalityContractCreditBonusStacks)));
		}

		return modifiers.Count == 0
			? ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>.Empty
			: new ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>(modifiers);
	}

	public static void InstallPlayerPipeline(
		StarSystemOrchestrator orchestrator,
		RunPilotRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(orchestrator);
		ArgumentNullException.ThrowIfNull(registry);
		if (orchestrator.PlayerId is null)
			return;

		var grants = registry.QueryCapabilitiesForActive(Supports);
		orchestrator.RuntimeFor(orchestrator.PlayerId).ReplaceActionEffects(Build(grants));
	}
}
