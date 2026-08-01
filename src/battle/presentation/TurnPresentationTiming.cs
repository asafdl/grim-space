using GrimSpace.Core.Log;

namespace GrimSpace.Battle.Presentation;

internal static class TurnPresentationTiming
{
	public static void LogPlanningReady(int turnNumber, double moveUiMs, double previewMs, double totalMs) =>
		GameLog.Log(
			$"Turn {turnNumber} planning ready: moveUi={moveUiMs:F1}ms "
			+ $"preview={previewMs:F1}ms total={totalMs:F1}ms");

	public static void LogResolveWait(int turnNumber, double elapsedMs) =>
		GameLog.Log($"Turn {turnNumber} resolve wait: {elapsedMs:F1}ms");
}
