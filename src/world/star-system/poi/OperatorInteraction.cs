using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Poi;

public sealed record OperatorInteraction
{
	public EFacilityOperatorRole Role { get; }
	public EMerchantSubtype? MerchantSubtype { get; }

	public OperatorInteraction(
		EFacilityOperatorRole role,
		EMerchantSubtype? merchantSubtype = null)
	{
		if (role == EFacilityOperatorRole.Merchant && merchantSubtype is null)
			throw new ArgumentException(
				"Merchant interactions require a merchant subtype.",
				nameof(merchantSubtype));
		if (role != EFacilityOperatorRole.Merchant && merchantSubtype is not null)
			throw new ArgumentException(
				$"Interaction role '{role}' cannot carry a merchant subtype.",
				nameof(merchantSubtype));

		Role = role;
		MerchantSubtype = merchantSubtype;
	}

	internal static OperatorInteraction FromLegacyTemplate(
		EFacilityOperatorRole role,
		EMerchantCatalog? catalog)
	{
		if (role != EFacilityOperatorRole.Merchant)
			return new OperatorInteraction(role);

		var subtype = catalog switch
		{
			EMerchantCatalog.Weapons => EMerchantSubtype.Weapons,
			EMerchantCatalog.ShipSupport => EMerchantSubtype.ShipSupport,
			EMerchantCatalog.Ships => EMerchantSubtype.Ships,
			EMerchantCatalog.Healing => throw new ArgumentException(
				"Merchant interactions cannot use the healing catalog.",
				nameof(catalog)),
			null => throw new ArgumentException(
				"Merchant interactions require a merchant catalog.",
				nameof(catalog)),
			_ => throw new ArgumentOutOfRangeException(nameof(catalog), catalog, null),
		};

		return new OperatorInteraction(role, subtype);
	}
}
