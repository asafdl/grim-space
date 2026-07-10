using GrimSpace.Battle.Actions.Enums;
using GrimSpace.Battle.Grid;

namespace GrimSpace.Battle.Movement;

public sealed class Option
{
	public ELateralDirection? Lateral { get; init; }
	public required IReadOnlyList<Coord> Path { get; init; }

	public Coord EndPosition => Path[^1];
}
