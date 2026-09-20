using GrimSpace.Units.Loadouts.Abilities;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Dockyard;

public enum EDockyardUpgradeCategory
{
	MaxShields,
	Ability,
}

public sealed record DockyardUpgradeOffer(
	string Id,
	EDockyardUpgradeCategory Category,
	AbilityMount? Mount,
	int RequiredShieldTier,
	AbilitySpec? RequiredAbilitySpec,
	ResourceBundle Cost);
