using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Movement;

public sealed class Option
{
	public int ApCost { get; init; }
	public required IReadOnlyList<Coord> Path { get; init; }
	public int StartMomentumLevel { get; init; }
	public int EndMomentumLevel { get; init; }

	public Coord EndPosition => Path[^1];

	public int PathLength => Path.Count;
}
