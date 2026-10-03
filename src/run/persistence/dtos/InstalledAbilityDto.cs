using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record InstalledAbilityDto(
	ESpatialOrientation MountedOn,
	AbilitySpecDto Spec);
