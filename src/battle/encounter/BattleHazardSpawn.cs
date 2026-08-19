using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Encounter;

public sealed class BattleHazardSpawn
{
	public required Coord Origin { get; init; }
	public required IReadOnlySet<Coord> Cells { get; init; }
}
