using GrimSpace.Math.Grid;
using GrimSpace.Units.Loadouts.Abilities;

namespace GrimSpace.Run.Persistence;

public sealed record MountRuntimeDto(
	EAbilityKind Kind,
	ESpatialOrientation MountedOn,
	int UsesRemaining,
	int CooldownRemaining);
