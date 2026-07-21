using System.Text;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Log;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using ShipOrientation = GrimSpace.Battle.Movement.Orientation;

namespace GrimSpace.Battle.Debug;

public static class StateLog
{
	public static void LogTurnResolution(
		int turnNumber,
		IReadOnlyList<IAction> actions,
		IReadOnlyList<Hazard> hazards,
		IReadOnlyDictionary<string, State> unitsAtTurnStart,
		IReadOnlyDictionary<string, State> unitsAfterPlayer,
		IReadOnlyDictionary<string, State> unitsAtTurnEnd)
	{
		var log = new StringBuilder();
		log.AppendLine($"=== Turn {turnNumber} ===");

		AppendSection(log, "Units (turn start)", unitsAtTurnStart.Values);

		log.AppendLine($"Turn actions ({actions.Count}):");
		if (actions.Count == 0)
			log.AppendLine("  (none)");
		else
		{
			for (var i = 0; i < actions.Count; i++)
				log.AppendLine($"  [{i}] {DescribeAction(actions[i])}");
		}

		AppendSection(log, "Units (after player phase)", unitsAfterPlayer.Values);

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
		GameLog.Log(log.ToString());
	}

	private static void AppendSection(StringBuilder log, string title, IEnumerable<State> units)
	{
		log.AppendLine(title + ":");
		foreach (var state in units)
			log.AppendLine($"  {FormatUnit(state)}");
	}

	private static string FormatUnit(State state) =>
		$"{state.Id}: pos={state.Position} fore={state.Fore} dorsal={state.Dorsal} "
		+ $"starboard={state.Starboard} mom={state.MomentumLevel} "
		+ $"hp={state.Hp}/{state.Stats.MaxHp} ap={state.ActionPoints}/{state.Stats.MaxAp}";

	private static string FormatPath(IReadOnlyList<Coord> path) =>
		path.Count == 0 ? "[]" : string.Join(" -> ", path);

	private static string DescribeAction(IAction action) =>
		$"{action.OwnerId}: {DescribeActionDetail(action)}";

	private static string DescribeActionDetail(IAction action) => action switch
	{
		MoveStepAction step => $"MoveStep {step.From} -> {step.To}",
		HeadingTurnAction heading => ShipOrientation.IsYawTurn(heading.Turn)
			? $"HeadingTurn {heading.Turn} (yaw, billed via turn state)"
			: $"HeadingTurn {heading.Turn} (1 AP)",
		RollAction roll => $"Roll {roll.Direction}",
		RailgunAction railgun => $"Railgun -> {railgun.TargetUnitId}",
		MissileAction missile => $"Missile {missile.Mount} @ {missile.Center} (range {missile.Range})",
		_ => action.GetType().Name,
	};
}
