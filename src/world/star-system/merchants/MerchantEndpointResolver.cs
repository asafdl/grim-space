using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantEndpointResolver
{
	public static MerchantEndpoint ForFacilityOperator(
		StarMap map,
		string poiId,
		string facilityId,
		string operatorName)
	{
		if (TryResolveLeadSaleContractId(map, poiId, facilityId, operatorName, out var contractId))
			return new LeadSaleMerchantEndpoint(contractId);

		return new FacilityMerchantEndpoint(poiId, facilityId, operatorName);
	}

	public static bool TryResolveLeadSaleContractId(
		StarMap map,
		string poiId,
		string facilityId,
		string operatorName,
		out string contractId)
	{
		contractId = "";
		if (!map.TryGetPointOfInterest(poiId, out var poi)
			|| !poi.OperatorTemporaryRoles.TryGetAssignment(
				facilityId,
				operatorName,
				out var assignment)
			|| assignment.Role != EFacilityOperatorRole.Merchant)
			return false;

		foreach (var sourceId in assignment.SourceIds)
		{
			if (!map.ContractRegistry.TryGet(sourceId, out var contract)
				|| contract.Category != EContractCategory.Lead
				|| !map.ContractRegistry.TryGetState(sourceId, out var state)
				|| state.Status != EContractStatus.Active)
				continue;

			contractId = sourceId;
			return true;
		}

		return false;
	}
}
