using GrimSpace.Math.Grid;

namespace GrimSpace.Run;

public sealed class WorldHazardSpawn
{
	public required Coord Origin { get; init; }
	public required IReadOnlySet<Coord> Cells { get; init; }
}
