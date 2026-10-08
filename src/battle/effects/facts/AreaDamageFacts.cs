using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct AreaDamageFacts(
	string SourceId,
	EImpactCause Cause,
	Coord Origin,
	IReadOnlySet<Coord> Cells);
