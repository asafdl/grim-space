using GrimSpace.Domain.Grid;
using GrimSpace.Domain.Units;

namespace GrimSpace.Domain.Run;

public sealed class Spawn
{
	public required Instance Unit { get; init; }
	public Coord Position { get; init; }
	public int InitialMomentum { get; init; }
}
