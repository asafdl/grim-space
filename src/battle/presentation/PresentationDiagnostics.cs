using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Diagnostic logging for presentation input, phase transitions, and async jobs.
/// </summary>
internal static class PresentationDiagnostics
{
	public static void LogPhaseTransition(PresentationPhase from, PresentationPhase to, string reason) =>
		GameLog.Log($"[presentation] phase {from} -> {to} ({reason})");

	public static void LogInputIgnored(PresentationPhase phase, string action) =>
		GameLog.Log($"[presentation] input ignored: {action} while phase={phase}");

	public static void LogMoveRejected(string reason, PresentationPhase phase, int optionIndex, int optionCount = -1)
	{
		var options = optionCount >= 0 ? $" options={optionCount}" : string.Empty;
		GameLog.Log(
			$"[presentation] move rejected: {reason} phase={phase} index={optionIndex}{options}");
	}

	public static void LogMovePickMiss(int optionCount) =>
		GameLog.Log($"[presentation] move pick miss: no option under cursor (options={optionCount})");

	public static void LogMoveQueueDetail(string reason, Coord? target = null)
	{
		var at = target is Coord cell ? $" target={cell}" : string.Empty;
		GameLog.Log($"[presentation] move queue failed: {reason}{at}");
	}

	public static void LogEndTurnIgnored(PresentationPhase phase) =>
		GameLog.Log($"[presentation] end turn ignored: phase={phase}");

	public static void LogCommitFailed() =>
		GameLog.Log("[presentation] end turn commit failed (invariant or battle over)");

	public static void LogPlanningHandoffAborted(string reason, PresentationPhase phase, bool hadPreparedMoveUi) =>
		GameLog.Log(
			$"[presentation] planning handoff aborted: {reason} phase={phase} "
			+ $"preparedMoveUi={hadPreparedMoveUi}");

	public static void LogResolveAborted(string reason, PresentationPhase phase) =>
		GameLog.Log($"[presentation] resolve handoff aborted: {reason} phase={phase}");

	public static void LogReplayNotifyIgnored(PresentationPhase phase) =>
		GameLog.Log($"[presentation] replay complete ignored: phase={phase}");

	public static void LogMovePrepSkipped(int turnNumber, string reason) =>
		GameLog.Log($"[presentation] move prep skipped turn={turnNumber}: {reason}");

	public static void LogJobStarted(string key, int version) =>
		GameLog.Log($"[presentation] job started: {key} v{version}");

	public static void LogJobCancelled(string key, int version) =>
		GameLog.Log($"[presentation] job cancelled: {key} -> v{version}");

	public static void LogJobAwaited(string key, int startedVersion, int currentVersion, bool hasTask, bool succeeded)
	{
		var stale = startedVersion == currentVersion ? "current" : "stale";
		GameLog.Log(
			$"[presentation] job awaited: {key} started=v{startedVersion} now=v{currentVersion} "
			+ $"hasTask={hasTask} {stale} succeeded={succeeded}");
	}

	public static void LogJobFailed(string key, Exception ex) =>
		GameLog.Log($"[presentation] job failed: {key} {ex.GetType().Name}: {ex.Message}");
}
