using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Effects;

namespace GrimSpace.World.StarSystem.Contracts;

internal static class DeliveryDiagnostics
{
	public static void RetryInterception(
		string contractId,
		string playerFleetId,
		int tick,
		EDeliveryInterceptionRetryReason reason,
		int delayTicks) =>
		GameLog.Log(
			$"[delivery] interception retry contract={contractId} player={playerFleetId} " +
			$"tick={tick} reason={reason} retryIn={delayTicks}");

	public static void AssignInterceptor(
		string contractId,
		string interceptorFleetId,
		string playerFleetId) =>
		GameLog.Log(
			$"[delivery] interceptor assigned contract={contractId} " +
			$"interceptor={interceptorFleetId} player={playerFleetId}");

	public static void ResolveInterception(string contractId, string interceptorFleetId) =>
		GameLog.Log(
			$"[delivery] interception resolved contract={contractId} interceptor={interceptorFleetId}");

	public static void Fail(string contractId, EDeliveryFailureReason reason) =>
		GameLog.Log($"[delivery] failed contract={contractId} reason={reason}");
}
