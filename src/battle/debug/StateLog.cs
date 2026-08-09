using System.Text;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Log;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using ShipOrientation = GrimSpace.Battle.Movement.Orientation;

namespace GrimSpace.Battle.Debug;

public static class StateLog
{
	public static void LogTurnResolution(
		int turnNumber,
		IReadOnlyList<ITimelineEntry> history,
		IReadOnlyList<Hazard> hazards,
		IReadOnlyDictionary<string, State> unitsAtTurnStart,
		IReadOnlyDictionary<string, State> unitsAfterPlayer,
		IReadOnlyDictionary<string, State> unitsAtTurnEnd)
	{
		var log = new StringBuilder();
		log.AppendLine($"=== Turn {turnNumber} ===");

		AppendSection(log, "Units (turn start)", unitsAtTurnStart.Values);

		log.AppendLine($"Turn history ({history.Count}):");
		if (history.Count == 0)
			log.AppendLine("  (none)");
		else
		{
			for (var i = 0; i < history.Count; i++)
				log.AppendLine($"  [{i}] {DescribeEntry(history[i])}");
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

	private static string DescribeEntry(ITimelineEntry entry) => entry switch
	{
		IAction action => DescribeAction(action),
		Record<ImpactFacts> { Value: var impact } =>
			$"Impact {impact.SourceId}->{impact.TargetId} {impact.Cause} face={impact.Face} "
			+ $"shield={impact.ShieldDamage} hull={impact.HullDamage} mom={impact.MomentumLoss}",
		Record<SpawnFacts> { Value: var spawn } =>
			$"Spawn {spawn.SourceId}->{spawn.TargetId} {spawn.EntityType}",
		_ => entry.GetType().Name,
	};

	private static string DescribeAction(IAction action) =>
		$"{action.ActorId}: {DescribeActionDetail(action)}";

	private static string DescribeActionDetail(IAction action) => action switch
	{
		MoveStepAction step => $"MoveStep {step.Direction}",
		HeadingTurnAction heading => ShipOrientation.IsYawTurn(heading.Turn)
			? $"HeadingTurn {heading.Turn} ({(heading.Turn == EHeadingTurn.Yaw180 ? 2 : 1)} AP)"
			: $"HeadingTurn {heading.Turn} (1 AP)",
		RollAction roll => $"Roll {roll.Direction}",
		RailgunAction => "Railgun",
		_ => action.GetType().Name,
	};
}
