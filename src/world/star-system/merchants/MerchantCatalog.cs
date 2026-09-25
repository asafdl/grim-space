using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class MerchantCatalog
{
	public enum Kind
	{
		InstallWeapon,
		UpgradeDamage,
		UpgradeRange,
		UpgradeMaxShields,
		UpgradeMaxHull,
		RepairHull,
		RechargeAllShields,
		RechargeShieldFace,
	}

	public readonly record struct Offering(
		Kind Kind,
		AbilityMount? Mount = null,
		ESpatialOrientation? Face = null);

	public sealed record Offer(Offering Offering, ResourceBundle Cost);

	public static bool TryFind(
		EMerchantCatalog catalog,
		Offering key,
		ShipInstance ship,
		out Offer offer)
	{
		var offers = catalog switch
		{
			EMerchantCatalog.Weapons => WeaponsCatalog.ListFor(ship),
			EMerchantCatalog.ShipSupport => ShipSupportCatalog.ListFor(ship),
			_ => [],
		};
		offer = offers.FirstOrDefault(x => x.Offering == key)!;
		return offer is not null;
	}
}
