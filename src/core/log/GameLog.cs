using System.Text;

namespace GrimSpace.Core.Log;

public static class GameLog
{
	private static Action<string>? _sink;
	private static Action<string>? _errorSink;

	public static void Configure(Action<string>? sink, Action<string>? errorSink = null)
	{
		_sink = sink;
		_errorSink = errorSink ?? sink;
	}

	public static void Log(string message) => _sink?.Invoke(message);

	public static void LogException(Exception ex, string? context = null)
	{
		var report = new StringBuilder();
		if (!string.IsNullOrWhiteSpace(context))
			report.AppendLine(context);
		report.AppendLine($"{ex.GetType().FullName}: {ex.Message}");
		if (!string.IsNullOrWhiteSpace(ex.StackTrace))
			report.Append(ex.StackTrace);
		_errorSink?.Invoke(report.ToString());
	}

	public static IDisposable BeginScope(Action<string> sink)
	{
		var previousSink = _sink;
		var previousErrorSink = _errorSink;
		_sink = sink;
		_errorSink = sink;
		return new Scope(previousSink, previousErrorSink);
	}

	private sealed class Scope(Action<string>? previousSink, Action<string>? previousErrorSink) : IDisposable
	{
		public void Dispose()
		{
			_sink = previousSink;
			_errorSink = previousErrorSink;
		}
	}
}
