using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Effects;

public readonly record struct GoopSpawnedFacts(
	string SourceId,
	string HazardId,
	Coord Center,
	IReadOnlySet<Coord> Cells);

public readonly record struct GoopDissipatedFacts(
	string SourceId,
	string HazardId);
