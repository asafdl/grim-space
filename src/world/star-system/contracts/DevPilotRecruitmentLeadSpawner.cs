using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Contracts;

/// <summary>Debug helper to add a pending pilot-hire black-market lead to the contract board.</summary>
public static class DevPilotRecruitmentLeadSpawner
{
	private const int DefaultDurationTicks = 500;

	public static bool TrySpawn(StarMap map, out string? failureReason)
	{
		ArgumentNullException.ThrowIfNull(map);
		failureReason = null;

		if (!MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Pilots))
		{
			failureReason = "Pilot recruitment is unavailable (no recruitable backgrounds configured).";
			return false;
		}

		var contractId = TypedIdGenerator.NextId("dev-pilot-lead");
		var args = new LeadCreateArgs(
			ELeadKind.BlackMarketSale,
			new ContractNarrative(
				"Pilot broker (dev)",
				"Debug-generated pilot hire lead. Accept to meet the seller."),
			DefaultDurationTicks,
			SpaceMeetingChance: 1.0,
			MerchantSubtype: EMerchantSubtype.Pilots);

		if (!ContractFactory.TryBuildLead(map, contractId, args, out var contract))
		{
			failureReason =
				"Could not place a black-market destination (no landmarks or eligible facility).";
			return false;
		}

		if (!map.ContractRegistry.TryAdd(contract))
		{
			failureReason = "Contract registry rejected the lead.";
			return false;
		}

		return true;
	}
}
