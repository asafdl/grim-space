using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Planning;

/// <summary>
/// Presentation queries: projects rules state into display data (highlights, ghosts, overlays).
/// </summary>
public static class View
{
	public static IReadOnlyList<Option> GetMoveHighlights(PlayerController planning, Unit? actor)
	{
		if (actor is null || !planning.CanAct(actor))
			return [];

		var board = PlanSimulator.Simulate(
			planning.Actor,
			planning.Opponent,
			planning.Grid,
			planning.Actions,
			planning.StartFacing,
			excludeMoves: true);

		return LegalActions.GetMoveOptions(board, planning.Context);
	}

	public static SimulatedTurn GetTurnGhost(PlayerController planning) =>
		BattlePlanExecutor.Simulate(planning.Actor, planning.Opponent, planning.Grid, planning.Plan);

	public static HashSet<Coord> GetPlannedHazardHighlights(PlayerController planning)
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in GetTurnGhost(planning).Hazards)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public static HashSet<Coord> GetMissileTargetHighlights(
		PlayerController planning,
		EMissileMount mount,
		int range)
	{
		var board = PlanSimulator.Simulate(
			planning.Actor,
			planning.Opponent,
			planning.Grid,
			planning.Actions,
			planning.StartFacing);

		return LegalActions.GetMissileCells(board, planning.Context, mount, range);
	}

	public static HashSet<Coord> GetMissileBlastHighlights(Coord center, BoundedGrid grid) =>
		Hazard.MissileZone(
			center,
			grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss).Cells;

	public static HashSet<Coord> GetRailgunTargetHighlights(PlayerController planning, Unit? actor)
	{
		var cells = new HashSet<Coord>();
		if (actor is null || !planning.CanAct(actor))
			return cells;

		var board = PlanSimulator.Simulate(
			planning.Actor,
			planning.Opponent,
			planning.Grid,
			planning.Actions,
			planning.StartFacing);

		if (!LegalActions.IsRailgunAvailable(board, planning.Context))
			return cells;

		cells.Add(board.Enemy.Position);
		return cells;
	}
}
