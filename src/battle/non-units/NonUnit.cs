using System.Collections.Frozen;
using GrimSpace.Battle.Spatial;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.NonUnits;

public abstract class NonUnit
{
	public required string Id { get; init; }
	public required string ActorId { get; init; }
	public required FrozenSet<Coord> Cells { get; init; }
	public required BodyFrame Frame { get; init; }

	public abstract IEnumerable<Coord> MovementBlockingCells { get; }
	public abstract IEnumerable<Coord> AbilityBlockingCells { get; }
}
