using GrimSpace.Application;
using GrimSpace.Core.Actions;
using GrimSpace.Run;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class MapSceneBinding : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private bool _disposed;

	public MapSceneBinding(StarSystemOrchestrator orchestrator)
	{
		_orchestrator = orchestrator;
		_orchestrator.WorldUpdated += OnWorldUpdated;
	}

	public event Action? WorldUpdated;

	public State Run => Session.Instance.Run;

	public StarMap Map => _orchestrator.Map;

	public bool TryCommitPlayerInput(IAction action) => _orchestrator.TryCommitPlayerInput(action);

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_orchestrator.WorldUpdated -= OnWorldUpdated;
		WorldUpdated = null;
	}

	private void OnWorldUpdated() => WorldUpdated?.Invoke();
}
