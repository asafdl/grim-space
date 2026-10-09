using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Run.Persistence;

public sealed record InstalledAbilityDto(
	ESpatialOrientation MountedOn,
	EAbilityKind? Kind = null,
	int DamageUpgradeTier = 0,
	int RangeUpgradeTier = 0,
	AbilitySpecDto? Spec = null);
