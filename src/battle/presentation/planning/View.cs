using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
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

		return Preview.GetLegalMoves(planning);
	}

	public static SimulatedTurn GetTurnGhost(PlayerController planning) =>
		Preview.Simulate(planning);

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
		int range) =>
		LegalActions.GetMissileCells(planning.Board, planning.Context, planning.OwnerId, mount, range);

	public static HashSet<Coord> GetMissileBlastHighlights(Coord center, BoundedGrid grid) =>
		Hazard.MissileZone(
			"preview",
			EntityIds.World,
			center,
			BodyFrame.WorldAligned(center),
			grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss).Cells;

	public static HashSet<Coord> GetRailgunTargetHighlights(PlayerController planning, Unit? actor)
	{
		var cells = new HashSet<Coord>();
		if (actor is null || !planning.CanAct(actor))
			return cells;

		var enemy = planning.Opponent;
		if (!LegalActions.IsRailgunAvailable(planning.Board, planning.Context, planning.OwnerId, enemy.State.Id))
			return cells;

		cells.Add(planning.Board.StateOf(enemy.State.Id).Position);
		return cells;
	}

	public static HashSet<Coord> GetFlakBurstHighlights(PlayerController planning, EFlakMount mount) =>
		LegalActions.GetFlakBurstCells(planning.Board, planning.Context, planning.OwnerId, mount);
}
