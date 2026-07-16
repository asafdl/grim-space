using GrimSpace.Math.Grid;

namespace GrimSpace.Run;

public sealed class AsteroidFieldConfig
{
	public required int Seed { get; init; }
	public int GridSize { get; init; } = 64;
	public IReadOnlyList<Coord> UnitPositions { get; init; } = [];
	public Coord RegionCenter { get; init; } = new(32, 32, 32);
	public int RegionHalfExtent { get; init; } = 9;
	public int RegionMargin { get; init; } = 2;
	public int TargetCount { get; init; } = 28;
	public int UnitClearance { get; init; } = 2;
	public int AsteroidGap { get; init; } = 1;
}
