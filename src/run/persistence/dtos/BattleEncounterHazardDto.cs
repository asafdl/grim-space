using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record BattleEncounterHazardDto(
	Coord Origin,
	IReadOnlyList<Coord> Cells,
	EBattleHazardKind Kind = EBattleHazardKind.Asteroid,
	BodyFrame? Frame = null);
