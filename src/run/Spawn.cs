// Placeholder until roguelike sector map exists.

using GrimSpace.Math.Grid;
using GrimSpace.Units;

namespace GrimSpace.Run;

public sealed class Spawn
{
	public required Instance Unit { get; init; }
	public Coord Position { get; init; }
	public int InitialMomentum { get; init; }
}
