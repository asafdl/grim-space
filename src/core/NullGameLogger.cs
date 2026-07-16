namespace GrimSpace.Core;

public sealed class NullGameLogger : IGameLogger
{
	public static NullGameLogger Instance { get; } = new();

	private NullGameLogger() { }

	public void Log(string message) { }
}
