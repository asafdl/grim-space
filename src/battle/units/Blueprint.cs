using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class Blueprint
{
	public required string Id { get; init; }
	public EType Type { get; init; }
	public EController Controller { get; init; }
	public Coord Position { get; init; }
}
