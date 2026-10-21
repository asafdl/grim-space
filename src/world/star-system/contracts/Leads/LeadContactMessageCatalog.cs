using GrimSpace.Math;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Contracts;

public static class LeadContactMessageCatalog
{
	public static string Pick(Contract contract, StarMap map)
	{
		ArgumentNullException.ThrowIfNull(contract);
		ArgumentNullException.ThrowIfNull(map);

		if (contract.Objective is not BlackMarketSaleObjective sale)
			throw new ArgumentException(
				$"Contract '{contract.Id}' is not a black-market lead.",
				nameof(contract));

		var destination = DestinationLabel(sale, map);
		var merchandise = MerchandiseLabel(sale.MerchantSubtype);
		var seed = StableSeedMixer.From(0).Add(contract.Id).Add("lead-contact-message").Value;
		var messages = ContractNarrativePickerConfig.DefaultEntries
			.Where(entry =>
				entry.Kind == EContractKind.Lead
				&& entry.Subtype == ContractNarrativeSubtype.LeadContact)
			.Select(entry => entry.Briefing)
			.ToArray();
		var index = (int)(seed % (ulong)messages.Length);
		return messages[index]
			.Replace("{destination}", destination, StringComparison.Ordinal)
			.Replace("{merchant}", merchandise, StringComparison.Ordinal);
	}

	private static string DestinationLabel(BlackMarketSaleObjective sale, StarMap map) =>
		sale.Destination switch
		{
			BlackMarketFacilityDestination facility =>
				ContractDisplay.ResolvePoiDisplayName(map, facility.PoiId),
			BlackMarketSpaceMeetingDestination meeting =>
				$"a rendezvous point: {ContractDisplay.FormatSearchAreaIntel(meeting.SearchArea.Intel, map)}",
			_ => throw new ArgumentOutOfRangeException(nameof(sale), sale.Destination, null),
		};

	private static string MerchandiseLabel(EMerchantSubtype subtype) =>
		subtype switch
		{
			EMerchantSubtype.Weapons => "weapons",
			EMerchantSubtype.ShipSupport => "shields",
			EMerchantSubtype.Ships => "ships",
			EMerchantSubtype.Pilots => "pilots",
			_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
		};
}
