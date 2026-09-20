using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Abilities;

public readonly record struct AbilityMount(
	EAbilityKind Kind,
	ESpatialOrientation Facet);
