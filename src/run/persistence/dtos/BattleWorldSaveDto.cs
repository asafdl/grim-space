using GrimSpace.Battle;
using GrimSpace.Battle.Objectives;
using GrimSpace.Math.Grid;

namespace GrimSpace.Run.Persistence;

public sealed record BattleWorldSaveDto(
	string BattleId,
	EObjective Objective,
	int GridWidth,
	int GridHeight,
	int GridDepth,
	IReadOnlyList<Coord> BlockedCells,
	IReadOnlyList<string> EngagedShipIds,
	EBattleResult BattleResult,
	IReadOnlyList<BattleUnitSaveDto> Units,
	IReadOnlyList<HazardSaveDto> Hazards,
	TimelineSnapshotDto Timeline);
