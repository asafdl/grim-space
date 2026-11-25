using GrimSpace.Core.Actions;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Runtime;
namespace GrimSpace.World.StarSystem.PilotCapabilities;

public static class StarMapPilotCapabilities
{
	public static EffectRules<StarMap, ActorRuntime> Rules { get; } =
		new([new ScrapRewardBonusRule(), new OptimalityContractCreditBonusRule()]);

	public static bool Supports(EPilotCapabilityId capabilityId) =>
		capabilityId is EPilotCapabilityId.MinorScrapRewardBonus
			or EPilotCapabilityId.ScrapRewardBonus
			or EPilotCapabilityId.OptimalityContractCreditBonus
			or EPilotCapabilityId.ExperienceGainBonus;

	public static void InstallPlayerCapabilities(
		StarSystemOrchestrator orchestrator,
		RunPilotRegistry registry)
	{
		ArgumentNullException.ThrowIfNull(orchestrator);
		ArgumentNullException.ThrowIfNull(registry);
		if (orchestrator.PlayerId is null)
			return;

		orchestrator.RuntimeFor(orchestrator.PlayerId).ReplaceStrategicCapabilities(
			registry.QueryCapabilitiesForAssigned(Supports));
	}
}
