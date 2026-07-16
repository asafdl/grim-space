using GrimSpace.Math.Grid;

namespace GrimSpace.Run;

public sealed class BoardHazardSpawn
{
	public required Coord Center { get; init; }
	public required int Radius { get; init; }
	public required string VisualId { get; init; }
}
