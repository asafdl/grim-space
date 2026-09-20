using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter;

public sealed class BattleSpawn
{
	public required ShipInstance Ship { get; init; }
	public required ETeam Team { get; init; }
	public Coord Position { get; init; }
	public Coord Fore { get; init; } = Coord.Forward;
	public Coord Dorsal { get; init; } = Coord.Up;

	public ExecutionAgent<BattleWorld, ActorRuntime> ExecutionAgent { get; init; } = null!;
}
