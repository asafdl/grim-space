using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Board;

public abstract class NonUnit
{
	public required string Id { get; init; }
	public required string OwnerId { get; init; }
	public required HashSet<Coord> Cells { get; init; }
	public required BodyFrame Frame { get; init; }
}
