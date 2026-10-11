using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public static class MerchantPurchaseValidation
{
	public static bool OperatorServesMerchantSubtype(
		StarMap map,
		string poiId,
		string facilityId,
		string operatorName,
		EMerchantSubtype subtype)
	{
		if (!TryResolveInteraction(
			    map,
			    poiId,
			    facilityId,
			    operatorName,
			    out var interaction))
			return false;

		return interaction.Role == EFacilityOperatorRole.Merchant
			&& interaction.MerchantSubtype == subtype;
	}

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
