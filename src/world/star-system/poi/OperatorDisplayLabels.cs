using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public enum EOperatorRoleIndicator
{
	None,
	Contract,
	Lead,
}

public static class OperatorDisplayLabels
{
	public static string Role(EFacilityOperatorRole role) => role switch
	{
		EFacilityOperatorRole.Contracts => "Contracts",
		EFacilityOperatorRole.StoryContact => "Story Contract",
		EFacilityOperatorRole.LeadContact => "Lead",
		EFacilityOperatorRole.Merchant => "",
		EFacilityOperatorRole.Dialog => "",
		EFacilityOperatorRole.DeliveryTurnIn => "Delivery",
		EFacilityOperatorRole.HealingStation => "Healing Station",
		_ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
	};

	public static EOperatorRoleIndicator Indicator(EFacilityOperatorRole role) =>
		role switch
		{
			EFacilityOperatorRole.Contracts or EFacilityOperatorRole.StoryContact =>
				EOperatorRoleIndicator.Contract,
			EFacilityOperatorRole.LeadContact => EOperatorRoleIndicator.Lead,
			_ => EOperatorRoleIndicator.None,
		};

	public static string MerchantRole(EMerchantCatalog catalog) => catalog switch
	{
		EMerchantCatalog.Weapons => "Dockyard Shop",
		EMerchantCatalog.ShipSupport => "Ship Support",
		EMerchantCatalog.Ships => "Ship Broker",
		EMerchantCatalog.Healing => throw new ArgumentException(
			"Healing stations do not use merchant role labels.",
			nameof(catalog)),
		_ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, null),
	};

	public static string MerchantRole(EMerchantSubtype subtype) => subtype switch
	{
		EMerchantSubtype.Weapons => "Dockyard Shop",
		EMerchantSubtype.ShipSupport => "Ship Support",
		EMerchantSubtype.Ships => "Ship Broker",
		_ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
	};

	public static string Title(FacilityOperator facilityOperator) =>
		Title(facilityOperator, facilityOperator.Interaction);

	public static string Title(
		FacilityOperator facilityOperator,
		OperatorInteraction interaction)
	{
		if (interaction.MerchantSubtype is { } subtype)
		{
			var role = MerchantRole(subtype);
			return $"{facilityOperator.Name} - {role}";
		}

		var roleLabel = Role(interaction.Role);
		return string.IsNullOrEmpty(roleLabel)
			? facilityOperator.Name
			: $"{facilityOperator.Name} - {roleLabel}";
	}
}
