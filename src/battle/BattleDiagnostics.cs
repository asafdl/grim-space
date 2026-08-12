using GrimSpace.Battle.Player;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;

namespace GrimSpace.Battle;

internal static class BattleDiagnostics
{
	public static void LogPhaseTransition(EBattlePhase from, EBattlePhase to, string reason) =>
		GameLog.Log($"[battle] phase {from} -> {to} ({reason})");

	public static void LogEndTurnIgnored(EBattlePhase phase) =>
		GameLog.Log($"[battle] end turn ignored: phase={phase}");

	public static void LogCommitFailed(
		bool battleOver,
		InvariantStatus invariantStatus,
		int turnNumber,
		int queuedActionCount) =>
		GameLog.Log(
			$"[battle] end turn commit failed: battleOver={battleOver} "
			+ $"invariant={invariantStatus} turn={turnNumber} queuedActions={queuedActionCount}");

	public static void LogResolveAborted(string reason, EBattlePhase phase) =>
		GameLog.Log($"[battle] resolve handoff aborted: {reason} phase={phase}");

	public static void LogReplayNotifyIgnored(EBattlePhase phase) =>
		GameLog.Log($"[battle] replay complete ignored: phase={phase}");

	public static void LogJobFailed(Exception ex) =>
		GameLog.LogException(ex, "[battle] resolve job failed");
}
