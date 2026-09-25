using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public static class OperatorDisplayLabels
{
	public static string Role(EFacilityOperatorRole role) => role switch
	{
		EFacilityOperatorRole.Contracts => "Contracts",
		EFacilityOperatorRole.Merchant => "",
		EFacilityOperatorRole.Dialog => "",
		EFacilityOperatorRole.DeliveryTurnIn => "Delivery",
		_ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
	};

	public static string MerchantRole(EMerchantCatalog catalog) => catalog switch
	{
		EMerchantCatalog.Weapons => "Dockyard Shop",
		EMerchantCatalog.ShipSupport => "Ship Support",
		_ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, null),
	};

	public static string Title(FacilityOperator facilityOperator)
	{
		if (facilityOperator.Role == EFacilityOperatorRole.Merchant
			&& facilityOperator.MerchantCatalog is { } catalog)
		{
			var role = MerchantRole(catalog);
			return $"{facilityOperator.Name} - {role}";
		}

		var templateRole = Role(facilityOperator.Role);
		return string.IsNullOrEmpty(templateRole)
			? facilityOperator.Name
			: $"{facilityOperator.Name} - {templateRole}";
	}
}
