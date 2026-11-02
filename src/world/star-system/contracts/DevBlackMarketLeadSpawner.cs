using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Contracts;

/// <summary>Debug helper to add a pending black-market sale lead to the contract board.</summary>
public static class DevBlackMarketLeadSpawner
{
	private const int DefaultDurationTicks = 500;

	public static bool TrySpawn(
		StarMap map,
		EMerchantSubtype subtype,
		out string contractId,
		out string? failureReason)
	{
		ArgumentNullException.ThrowIfNull(map);
		contractId = "";
		failureReason = null;

		if (!MerchantSubtypeStock.HasOffers(map, subtype))
		{
			failureReason = $"Black-market {subtype} stock is unavailable.";
			return false;
		}

		var id = TypedIdGenerator.NextId($"dev-{subtype.ToString().ToLowerInvariant()}-lead");
		var args = new LeadCreateArgs(
			ELeadKind.BlackMarketSale,
			new ContractNarrative(
				$"{subtype} broker (dev)",
				"Debug-generated black-market lead. Accept to meet the seller."),
			DefaultDurationTicks,
			SpaceMeetingChance: 1.0,
			MerchantSubtype: subtype);

		if (!ContractFactory.TryBuildLead(map, id, args, out var contract))
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

		contractId = id;
		return true;
	}
}
