using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public static class MerchantPurchaseValidation
{
	public static bool OperatorServesCatalog(
		StarMap map,
		string poiId,
		string facilityId,
		string operatorName,
		EMerchantCatalog catalog)
	{
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

		return facilityOperator.Role == EFacilityOperatorRole.Merchant
			&& facilityOperator.MerchantCatalog == catalog;
	}
}
