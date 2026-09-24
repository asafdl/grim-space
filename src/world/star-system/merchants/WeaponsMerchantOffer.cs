using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public enum EWeaponsOfferCategory
{
	MaxShields,
	Ability,
}

public sealed record WeaponsMerchantOffer(
	string Id,
	EWeaponsOfferCategory Category,
	AbilityMount? Mount,
	int RequiredShieldTier,
	AbilitySpec? RequiredAbilitySpec,
	ResourceBundle Cost);
