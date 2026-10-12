using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public static class MerchantPurchaseValidation
{
	public static bool EndpointServesMerchantSubtype(
		StarMap map,
		MerchantEndpoint endpoint,
		EMerchantSubtype subtype) =>
		TryResolveMerchantSubtype(map, endpoint, out var resolved)
		&& resolved == subtype;

	public static bool EndpointServesHealingStation(StarMap map, MerchantEndpoint endpoint) =>
		endpoint is FacilityMerchantEndpoint facility
		&& OperatorServesHealingStation(
			map,
			facility.PoiId,
			facility.FacilityId,
			facility.OperatorName);

	public static bool OperatorServesMerchantSubtype(
		StarMap map,
		string poiId,
		string facilityId,
		string operatorName,
		EMerchantSubtype subtype) =>
		EndpointServesMerchantSubtype(
			map,
			new FacilityMerchantEndpoint(poiId, facilityId, operatorName),
			subtype);

	public static bool OperatorServesHealingStation(
		StarMap map,
		string poiId,
		string facilityId,
		string operatorName) =>
		TryResolveInteraction(
			map,
			poiId,
			facilityId,
			operatorName,
			out var interaction)
		&& interaction.Role == EFacilityOperatorRole.HealingStation;

	public static bool TryResolveMerchantSubtype(
		StarMap map,
		MerchantEndpoint endpoint,
		out EMerchantSubtype subtype)
	{
		subtype = default;
		return endpoint switch
		{
			FacilityMerchantEndpoint facility => TryResolveFacilityMerchantSubtype(
				map,
				facility,
				out subtype),
			LeadSaleMerchantEndpoint lead => TryResolveLeadSaleMerchantSubtype(
				map,
				lead,
				out subtype),
			_ => false,
		};
	}

	private static bool TryResolveFacilityMerchantSubtype(
		StarMap map,
		FacilityMerchantEndpoint endpoint,
		out EMerchantSubtype subtype)
	{
		subtype = default;
		if (!TryResolveInteraction(
			    map,
			    endpoint.PoiId,
			    endpoint.FacilityId,
			    endpoint.OperatorName,
			    out var interaction))
			return false;

		if (interaction.Role != EFacilityOperatorRole.Merchant
			|| interaction.MerchantSubtype is not { } merchantSubtype)
			return false;

		subtype = merchantSubtype;
		return true;
	}

	private static bool TryResolveLeadSaleMerchantSubtype(
		StarMap map,
		LeadSaleMerchantEndpoint endpoint,
		out EMerchantSubtype subtype)
	{
		subtype = default;
		if (!map.ContractRegistry.TryGet(endpoint.ContractId, out var contract)
			|| contract.Category != EContractCategory.Lead
			|| contract.Objective is not BlackMarketSaleObjective sale
			|| !map.ContractRegistry.TryGetState(endpoint.ContractId, out var state)
			|| state is not LeadContractState lead)
			return false;

		if (state.Status != EContractStatus.Active)
			return false;

		if (map.Timeline.Clock.Current > lead.DeadlineTick)
			return false;

		if (!LeadSalePresentationIsActive(map, sale, lead))
			return false;

		subtype = sale.MerchantSubtype;
		return true;
	}

	private static bool LeadSalePresentationIsActive(
		StarMap map,
		BlackMarketSaleObjective sale,
		LeadContractState lead)
	{
		switch (sale.Destination)
		{
			case BlackMarketFacilityDestination facility:
				if (lead.DestinationOperatorName is not { } operatorName)
					return false;

				if (!map.TryGetPointOfInterest(facility.PoiId, out var poi))
					return false;

				return poi.OperatorTemporaryRoles.TryGetAssignment(
					facility.FacilityId,
					operatorName,
					out var assignment)
					&& assignment.Role == EFacilityOperatorRole.Merchant
					&& assignment.SourceIds.Contains(lead.ContractId);
			case BlackMarketSpaceMeetingDestination meeting:
				return map.FleetRegistry.TryGet(meeting.MeetingId, out var fleet)
					&& string.Equals(
						fleet.State.SourceContractId,
						lead.ContractId,
						StringComparison.Ordinal);
			default:
				return false;
		}
	}

	private static bool TryResolveInteraction(
		StarMap map,
		string poiId,
		string facilityId,
		string operatorName,
		out OperatorInteraction interaction)
	{
		interaction = null!;
		if (!map.TryGetPointOfInterest(poiId, out var poi))
			return false;

		Facility facility;
		try
		{
			facility = poi.GetFacility(facilityId);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		var facilityOperator = facility.Operators.FirstOrDefault(candidate =>
			string.Equals(candidate.Name, operatorName, StringComparison.OrdinalIgnoreCase));
		if (facilityOperator is null)
			return false;

		interaction = poi.ResolveInteraction(
			facilityId,
			facilityOperator.Name,
			facilityOperator.Interaction);
		return true;
	}
}
