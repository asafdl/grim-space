using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Presentation.Facilities;

/// <summary>Production copy for pilot black-market hire surfaces (facility merchant and map meeting).</summary>
public static class PilotSalePresentationLabels
{
	public static string Title(StarMap map, string contractId)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);

		if (!map.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not BlackMarketSaleObjective sale
			|| sale.MerchantSubtype != EMerchantSubtype.Pilots)
		{
			throw new InvalidOperationException(
				$"Contract '{contractId}' is not an active pilot black-market sale lead.");
		}

		if (sale.Destination is BlackMarketSpaceMeetingDestination)
			return $"Black market — {sale.MerchantSubtype}";

		if (sale.Destination is not BlackMarketFacilityDestination facilityDestination)
			throw new InvalidOperationException(
				$"Contract '{contractId}' has an unsupported lead destination.");

		if (!map.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not LeadContractState lead
			|| string.IsNullOrEmpty(lead.DestinationOperatorName))
		{
			throw new InvalidOperationException(
				$"Pilot sale '{contractId}' must be accepted at a facility before opening hire UI.");
		}

		var operatorName = lead.DestinationOperatorName;
		var poi = map.GetPointOfInterest(facilityDestination.PoiId);
		var facility = poi.GetFacility(facilityDestination.FacilityId);
		var facilityOperator = facility.Operators.First(operatorEntry =>
			string.Equals(operatorEntry.Name, operatorName, StringComparison.Ordinal));

		if (!poi.OperatorTemporaryRoles.TryGetInteraction(
				facility.Id,
				operatorName,
				out var interaction))
		{
			throw new InvalidOperationException(
				$"Lead merchant interaction is missing for operator '{operatorName}'.");
		}

		return OperatorDisplayLabels.Title(facilityOperator, interaction);
	}
}
