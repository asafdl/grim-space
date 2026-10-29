using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct HealFacts(
	string SourceId,
	string TargetId,
	EImpactCause Cause,
	ESpatialOrientation ShieldFace,
	int ShieldRestored,
	int HullRestored);
