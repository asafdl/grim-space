using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;

namespace GrimSpace.Battle.Encounter;

public sealed class BattleSpawn
{
	public required Instance Unit { get; init; }
	public Coord Position { get; init; }
	public int InitialMomentum { get; init; }
	public Coord Fore { get; init; } = Coord.Forward;
	public Coord Dorsal { get; init; } = Coord.Up;

	public ExecutionAgent<BattleWorld, ActorRuntime> ExecutionAgent { get; init; } = null!;
}
