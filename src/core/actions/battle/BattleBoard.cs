using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

/// <summary>
/// Mutable battle scene state. Simulation uses clones; commit uses live references.
/// </summary>
public sealed class BattleBoard
{
	public required State Player { get; init; }
	public required State Enemy { get; init; }
	public required Unit PlayerUnit { get; init; }
	public required BoundedGrid Grid { get; init; }
	public required ICollection<Hazard> Hazards { get; init; }

	public static BattleBoard ForSimulation(Unit player, Unit enemy, BoundedGrid grid) =>
		new()
		{
			Player = player.State.Clone(),
			Enemy = enemy.State.Clone(),
			PlayerUnit = player,
			Grid = grid,
			Hazards = [],
		};

	public static BattleBoard ForCommit(
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		ICollection<Hazard> hazards) =>
		new()
		{
			Player = player.State,
			Enemy = enemy.State,
			PlayerUnit = player,
			Grid = grid,
			Hazards = hazards,
		};
}
