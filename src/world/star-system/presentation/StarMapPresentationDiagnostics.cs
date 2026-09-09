using GrimSpace.Core.Actions;
using GrimSpace.Core.Log;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Diagnostic logging for star-map presentation input and player action enqueue/commit.
/// </summary>
internal static class StarMapPresentationDiagnostics
{
	public static void LogActionQueued(IAction action, StarMapPlayerExecutionAgent agent) =>
		GameLog.Log(
			$"[star-map] action queued: {DescribeAction(action)} {DescribeAgent(agent)}");

	public static void LogActionRejected(IAction action, string reason, StarMapPlayerExecutionAgent agent) =>
		GameLog.Log(
			$"[star-map] action rejected: {DescribeAction(action)} reason={reason} {DescribeAgent(agent)}");

	public static void LogCommitSkipped(string reason, StarMapPlayerExecutionAgent agent) =>
		GameLog.Log($"[star-map] commit skipped: reason={reason} {DescribeAgent(agent)}");

	public static void LogActionCommitted(IAction action, StarMapPlayerExecutionAgent agent) =>
		GameLog.Log(
			$"[star-map] action committed: {DescribeAction(action)} {DescribeAgent(agent)}");

	public static void LogInputRejected(IAction action, string reason) =>
		GameLog.Log($"[star-map] input rejected: {DescribeAction(action)} reason={reason}");

	public static void LogInputCommitted(IAction action, bool advancedClock) =>
		GameLog.Log(
			$"[star-map] input committed: {DescribeAction(action)} advancedClock={advancedClock}");

	public static void LogMoveQueueFailed(string reason, Coord? target, StarMapPlayerExecutionAgent agent)
	{
		var at = target is Coord cell ? $" target={cell}" : string.Empty;
		GameLog.Log(
			$"[star-map] move queue failed: reason={reason}{at} {DescribeAgent(agent)}");
	}

	public static void LogMovePickMiss() =>
		GameLog.Log("[star-map] move pick miss: no map cell under cursor");

	public static void LogCourseQueued(string kind, Coord? target, StarMapPlayerExecutionAgent agent)
	{
		var at = target is Coord cell ? $" target={cell}" : string.Empty;
		GameLog.Log($"[star-map] course queued: kind={kind}{at} {DescribeAgent(agent)}");
	}

	private static string DescribeAction(IAction action) => action switch
	{
		MoveAction move => $"move actor={move.ActorId} dest={move.Destination}",
		HuntUnitAction hunt =>
			$"hunt actor={hunt.ActorId} target={hunt.TargetUnitId} dest={hunt.Destination}",
		EngageAction engage => $"engage actor={engage.ActorId}",
		FleeAction flee => $"flee actor={flee.ActorId}",
		AcceptContractAction accept =>
			$"accept_contract actor={accept.ActorId} contract={accept.ContractId}",
		DeclineContractAction decline =>
			$"decline_contract actor={decline.ActorId} contract={decline.ContractId}",
		_ => $"{action.GetType().Name} actor={action.ActorId}",
	};

	private static string DescribeAgent(StarMapPlayerExecutionAgent agent) =>
		$"planning={agent.IsPlanning} pending={agent.HasPendingAction} canWork={agent.CanWorkForDiagnostics}";
}
