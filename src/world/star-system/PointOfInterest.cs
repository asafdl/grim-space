using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem;

public sealed class PointOfInterest
{
	public required string Id { get; init; }
	public required IReadOnlySet<Coord> Cells { get; init; }
}
