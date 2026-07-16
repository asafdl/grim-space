namespace GrimSpace.Core;

public static class GameLog
{
	public static IGameLogger Logger { get; set; } = NullGameLogger.Instance;
}
