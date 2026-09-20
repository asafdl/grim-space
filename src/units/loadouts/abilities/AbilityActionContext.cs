using GrimSpace.Math.Grid;

namespace GrimSpace.Units.Loadouts.Abilities;

public readonly record struct AbilityActionContext(
	AbilitySpec Spec,
	IReadOnlyList<ESpatialOrientation> Facets);
