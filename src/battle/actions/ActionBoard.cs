using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Mutable board state that actions read and write. Simulation uses clones; commit uses live references.
/// </summary>
public sealed class ActionBoard
{
	public required State Player { get; init; }
	public required State Enemy { get; init; }
	public required Unit PlayerUnit { get; init; }
	public required BoundedGrid Grid { get; init; }
	public required ICollection<Hazard> Hazards { get; init; }

	public static ActionBoard ForSimulation(Unit player, Unit enemy, BoundedGrid grid) =>
		new()
		{
			Player = CloneState(player.State),
			Enemy = CloneState(enemy.State),
			PlayerUnit = player,
			Grid = grid,
			Hazards = [],
		};

	private static State CloneState(State source) =>
		new()
		{
			Id = source.Id,
			Position = source.Position,
			ForwardDirection = source.ForwardDirection,
			UpDirection = source.UpDirection,
			RightDirection = source.RightDirection,
			ActionPoints = source.ActionPoints,
			Hp = source.Hp,
			MomentumLevel = source.MomentumLevel,
			Stats = source.Stats,
		};

	public static ActionBoard ForCommit(
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
