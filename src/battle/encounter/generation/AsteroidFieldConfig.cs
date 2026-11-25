using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Encounter.Generation;

public sealed class AsteroidFieldConfig
{
	public required int Seed { get; init; }
	public required int GridSize { get; init; }
	public IReadOnlyList<Coord> UnitPositions { get; init; } = [];
	public required Coord RegionCenter { get; init; }
	public required int RegionHalfExtent { get; init; }
	public int RegionMargin { get; init; } = 2;
	public required int TargetCount { get; init; }
	public int UnitClearance { get; init; } = 2;
	public int AsteroidGap { get; init; } = 1;
	public IReadOnlySet<Coord> ReservedCells { get; init; } = new HashSet<Coord>();
}
