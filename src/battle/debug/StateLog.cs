using System.Text;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Presentation;
using GrimSpace.Core.Log;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Debug;

public static class StateLog
{
	public static void LogTurnResolution(
		int turnNumber,
		IReadOnlyList<ITimelineEntry> history,
		IReadOnlyList<Hazard> hazards,
		IReadOnlyDictionary<string, State> unitsAtTurnStart,
		IReadOnlyDictionary<string, State> unitsAfterPlayer,
		IReadOnlyDictionary<string, State> unitsAtTurnEnd,
		Func<string, string> displayName)
	{
		var log = new StringBuilder();
		log.AppendLine($"=== Turn {turnNumber} ===");

		AppendSection(log, "Units (turn start)", unitsAtTurnStart.Values);

		var actionLines = ActionLog.Format(history, displayName);
		log.AppendLine($"Action log ({actionLines.Count}):");
		if (actionLines.Count == 0)
			log.AppendLine("  (none)");
		else
		{
			foreach (var line in actionLines)
				log.AppendLine($"  {line}");
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
		+ $"hull={state.HullPoints}/{state.Stats.MaxHullPoints} "
		+ $"shields=F{state.ShieldPoints[ESpatialOrientation.Forward]}"
		+ $"/A{state.ShieldPoints[ESpatialOrientation.Retro]}"
		+ $"/S{state.ShieldPoints[ESpatialOrientation.Starboard]}"
		+ $"/P{state.ShieldPoints[ESpatialOrientation.Port]}"
		+ $"/D{state.ShieldPoints[ESpatialOrientation.Dorsal]}"
		+ $"/V{state.ShieldPoints[ESpatialOrientation.Ventral]} "
		+ $"ap={state.ActionPoints}/{state.Stats.MaxAp}";
}
