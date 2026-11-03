using System.Text;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Presentation;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Debug;

public static class StateLog
{
	public static void LogTurnResolution(
		int turnNumber,
		IReadOnlyList<ITimelineEntry> history,
		IReadOnlyDictionary<string, State> unitsAtTurnStart,
		IReadOnlyDictionary<string, State> unitsAfterPlayer,
		IReadOnlyDictionary<string, State> unitsAtTurnEnd,
		Func<string, string> displayName)
	{
		var log = new StringBuilder();
		log.AppendLine($"=== Turn {turnNumber} ===");

		AppendSection(log, "Units (turn start)", unitsAtTurnStart.Values);

		var actionEntries = ActionLog.Format(history, displayName);
		log.AppendLine($"Action log ({actionEntries.Count}):");
		if (actionEntries.Count == 0)
			log.AppendLine("  (none)");
		else
		{
			foreach (var entry in actionEntries)
			{
				log.AppendLine($"  {entry.Title}");
				foreach (var metadata in entry.Metadata)
					log.AppendLine($"    {metadata}");
			}
		}

		AppendSection(log, "Units (after player phase)", unitsAfterPlayer.Values);

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
		+ $"starboard={state.Starboard} "
		+ $"hull={state.CurrentHullPoints}/{state.Loadout.BaseHullPoints} "
		+ $"shields=F{state.CurrentShieldPoints[ESpatialOrientation.Forward]}"
		+ $"/A{state.CurrentShieldPoints[ESpatialOrientation.Retro]}"
		+ $"/S{state.CurrentShieldPoints[ESpatialOrientation.Starboard]}"
		+ $"/P{state.CurrentShieldPoints[ESpatialOrientation.Port]}"
		+ $"/D{state.CurrentShieldPoints[ESpatialOrientation.Dorsal]}"
		+ $"/V{state.CurrentShieldPoints[ESpatialOrientation.Ventral]} "
		+ $"ap={state.CurrentActionPoints}/{state.Maneuverability.BaseActionPoints} "
		+ $"mp={state.CurrentManeuverPoints}/{state.Maneuverability.BaseManeuverPoints}";
}
