using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public static class OperatorDisplayLabels
{
	public static string Role(EFacilityOperatorRole role) => role switch
	{
		EFacilityOperatorRole.Contracts => "Contracts",
		EFacilityOperatorRole.StoryContact => "Story Contract",
		EFacilityOperatorRole.Merchant => "",
		EFacilityOperatorRole.Dialog => "",
		EFacilityOperatorRole.DeliveryTurnIn => "Delivery",
		EFacilityOperatorRole.HealingStation => "Healing Station",
		_ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
	};

	public static string MerchantRole(EMerchantCatalog catalog) => catalog switch
	{
		EMerchantCatalog.Weapons => "Dockyard Shop",
		EMerchantCatalog.ShipSupport => "Ship Upgrades",
		EMerchantCatalog.Ships => "Ship Broker",
		EMerchantCatalog.Healing => throw new ArgumentException(
			"Healing stations do not use merchant role labels.",
			nameof(catalog)),
		_ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, null),
	};

	public static string Title(FacilityOperator facilityOperator) =>
		Title(facilityOperator, facilityOperator.Role);

	public static string Title(
		FacilityOperator facilityOperator,
		EFacilityOperatorRole resolvedRole)
	{
		if (resolvedRole == EFacilityOperatorRole.Merchant
			&& facilityOperator.MerchantCatalog is { } catalog)
		{
			var role = MerchantRole(catalog);
			return $"{facilityOperator.Name} - {role}";
		}

		var roleLabel = Role(resolvedRole);
		return string.IsNullOrEmpty(roleLabel)
			? facilityOperator.Name
			: $"{facilityOperator.Name} - {roleLabel}";
	}
}
