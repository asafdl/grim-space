namespace GrimSpace.Core.Log;

public static class GameLog
{
	private static Action<string>? _sink;

	public static void Configure(Action<string>? sink) => _sink = sink;

	public static void Log(string message) => _sink?.Invoke(message);

	public static IDisposable BeginScope(Action<string> sink)
	{
		var previous = _sink;
		_sink = sink;
		return new Scope(previous);
	}

	private sealed class Scope(Action<string>? previous) : IDisposable
	{
		public void Dispose() => _sink = previous;
	}
}
