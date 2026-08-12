using GrimSpace.Battle;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

/// <summary>
/// Diagnostic logging for presentation input, phase transitions, and async jobs.
/// </summary>
internal static class PresentationDiagnostics
{
	private static string? _lastMovePreviewFingerprint;
	private static string? _lastMoveHighlightFingerprint;

	public static void LogInputIgnored(EBattlePhase phase, string action) =>
		GameLog.Log($"[presentation] input ignored: {action} while phase={phase}");

	public static void LogMoveRejected(string reason, EBattlePhase phase, int optionIndex, int optionCount = -1)
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

	public static void LogPlanningHandoffAborted(string reason, EBattlePhase phase, bool hadPreparedMoveUi) =>
		GameLog.Log(
			$"[presentation] planning handoff aborted: {reason} phase={phase} "
			+ $"preparedMoveUi={hadPreparedMoveUi}");

	/// <summary>
	/// Logs move-preview endpoints when the shown set (or gate reason) changes. Skips hover-only noise.
	/// Full cell diffs live in <c>[move-preview] discover</c> logs.
	/// </summary>
	public static void LogMovePreview(
		int turnNumber,
		string source,
		EPlayerMode mode,
		bool acceptsCommands,
		bool hasPlanningActor,
		bool canAct,
		bool weaponQueued,
		Coord actorPos,
		int pathApBaseline,
		int queuedActionCount,
		IReadOnlyList<MovePathOption> paths)
	{
		var gate = ResolveMovePreviewGate(
			mode,
			acceptsCommands,
			hasPlanningActor,
			canAct,
			weaponQueued,
			paths.Count);
		var fingerprint =
			$"{turnNumber}|{source}|{mode}|{gate}|{pathApBaseline}|{queuedActionCount}|{paths.Count}";
		if (fingerprint == _lastMovePreviewFingerprint)
			return;

		_lastMovePreviewFingerprint = fingerprint;
		GameLog.Log(
			$"[presentation] move preview: turn={turnNumber} source={source} mode={mode} "
			+ $"gate={gate} actor={actorPos} baselineAp={pathApBaseline} queued={queuedActionCount} "
			+ $"paths={paths.Count}");
	}

	public static void LogMovePreviewHighlights(int pathCount, int endpointCount)
	{
		// Hover redraws call SetMoveHighlights with a new target; only log endpoint-set changes.
		var fingerprint = $"{pathCount}|{endpointCount}";
		if (fingerprint == _lastMoveHighlightFingerprint)
			return;

		_lastMoveHighlightFingerprint = fingerprint;
		GameLog.Log(
			$"[presentation] move highlights: paths={pathCount} endpoints={endpointCount}");
	}

	private static string ResolveMovePreviewGate(
		EPlayerMode mode,
		bool acceptsCommands,
		bool hasPlanningActor,
		bool canAct,
		bool weaponQueued,
		int pathCount)
	{
		if (mode != EPlayerMode.Move)
			return $"mode_{mode}";
		if (!acceptsCommands)
			return "not_accepting_commands";
		if (!hasPlanningActor)
			return "no_planning_actor";
		if (!canAct)
			return "cannot_act";
		if (pathCount == 0)
			return weaponQueued ? "discovery_empty_after_weapon" : "discovery_empty";
		return weaponQueued ? "ok_with_weapon" : "ok";
	}

	public static void LogResolveAborted(string reason, EBattlePhase phase) =>
		GameLog.Log($"[presentation] resolve handoff aborted: {reason} phase={phase}");

	public static void LogReplayNotifyIgnored(EBattlePhase phase) =>
		GameLog.Log($"[presentation] replay complete ignored: phase={phase}");

	public static void LogMovePrepSkipped(int turnNumber, string reason) =>
		GameLog.Log($"[presentation] move prep skipped turn={turnNumber}: {reason}");

	public static void LogJobFailed(string key, Exception ex) =>
		GameLog.LogException(ex, $"[presentation] job failed: {key}");
}
