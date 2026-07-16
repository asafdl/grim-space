using GrimSpace.Core;

namespace GrimSpace.Tests;

internal sealed class GameLogTestScope : IDisposable
{
	private readonly IGameLogger _previous;

	public GameLogTestScope(IGameLogger logger)
	{
		_previous = GameLog.Logger;
		GameLog.Logger = logger;
	}

	public void Dispose() => GameLog.Logger = _previous;
}
