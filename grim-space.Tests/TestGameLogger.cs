using GrimSpace.Core;

namespace GrimSpace.Tests;

internal sealed class TestGameLogger : IGameLogger
{
	public List<string> Messages { get; } = [];

	public void Log(string message) => Messages.Add(message);
}
