using System.Text;
using Godot;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using ShipOrientation = GrimSpace.Battle.Movement.Orientation;

namespace GrimSpace.Battle.Debug;

public static class StateLog
{
	public static void LogTurnResolution(
		int turnNumber,
		IReadOnlyList<IBattleAction> plannedActions,
		IReadOnlyList<IBattleAction> enemyActions,
		IReadOnlyList<Hazard> hazards,
		IReadOnlyDictionary<string, State> unitsAtTurnStart,
		IReadOnlyDictionary<string, State> unitsAfterPlayer,
		IReadOnlyDictionary<string, State> unitsAfterEnemy,
		IReadOnlyDictionary<string, State> unitsAtTurnEnd)
	{
		var log = new StringBuilder();
		log.AppendLine($"=== Turn {turnNumber} ===");

		AppendSection(log, "Units (turn start)", unitsAtTurnStart.Values);

		log.AppendLine("Player plan:");
		if (plannedActions.Count == 0)
			log.AppendLine("  (none)");
		else
		{
			for (var i = 0; i < plannedActions.Count; i++)
				log.AppendLine($"  [{i}] {DescribeAction(plannedActions[i])}");
		}

		AppendSection(log, "Units (after player plan)", unitsAfterPlayer.Values);

		if (enemyActions.Count == 0)
			log.AppendLine("Enemy actions: (none)");
		else
		{
			log.AppendLine("Enemy actions:");
			for (var i = 0; i < enemyActions.Count; i++)
				log.AppendLine($"  [{i}] {DescribeAction(enemyActions[i])}");
		}

		AppendSection(log, "Units (after enemy turn)", unitsAfterEnemy.Values);

		if (hazards.Count == 0)
			log.AppendLine("Active hazards: (none)");
		else
		{
			log.AppendLine($"Active hazards ({hazards.Count}):");
			for (var i = 0; i < hazards.Count; i++)
			{
				var hazard = hazards[i];
				log.AppendLine(
					$"  [{i}] center={hazard.Center} dmg={hazard.Damage} momLoss={hazard.MomentumLoss} cells={hazard.Cells.Count}");
			}
		}

		AppendSection(log, "Units (turn end)", unitsAtTurnEnd.Values);
		GD.Print(log.ToString());
	}

	private static void AppendSection(StringBuilder log, string title, IEnumerable<State> units)
	{
		log.AppendLine(title + ":");
		foreach (var state in units)
			log.AppendLine($"  {FormatUnit(state)}");
	}

	private static string FormatUnit(State state) =>
		$"{state.Id}: pos={state.Position} fwd={state.ForwardDirection} up={state.UpDirection} "
		+ $"right={state.RightDirection} mom={state.MomentumLevel} "
		+ $"hp={state.Hp}/{state.Stats.MaxHp} ap={state.ActionPoints}/{state.Stats.MaxAp}";

	private static string FormatPath(IReadOnlyList<Coord> path) =>
		path.Count == 0 ? "[]" : string.Join(" -> ", path);

	private static string DescribeAction(IBattleAction action) => action switch
	{
		MoveAction move => $"Move ap={move.Option.ApCost} path={FormatPath(move.Option.Path)}",
		HeadingTurnAction heading => ShipOrientation.IsYawTurn(heading.Turn)
			? $"HeadingTurn {heading.Turn} (yaw, settled on commit)"
			: $"HeadingTurn {heading.Turn} (1 AP)",
		RollAction roll => $"Roll {roll.Direction}",
		RailgunAction railgun => $"Railgun -> {railgun.TargetUnitId}",
		MissileAction missile => $"Missile {missile.Mount} @ {missile.Center} (range {missile.Range})",
		_ => action.GetType().Name,
	};
}
