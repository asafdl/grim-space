using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Run.Persistence;

public sealed record InstalledAbilityDto(
	ESpatialOrientation MountedOn,
	EAbilityKind Kind,
	int DamageUpgradeTier,
	int RangeUpgradeTier);
