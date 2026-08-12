using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Player;

public readonly record struct HumanTurnContext(
	string HumanActorId,
	int TurnNumber,
	bool CanAct,
	IReadOnlySet<Coord> HazardCells);
