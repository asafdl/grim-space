using GrimSpace.Application;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.World.StarSystem.Presentation.Scene;

public sealed class MapSceneBinding : IDisposable
{
	private readonly StarSystemOrchestrator _orchestrator;
	private readonly IDisposable _catalogQuoteSubscription;
	private bool _disposed;

	public MapSceneBinding(StarSystemOrchestrator orchestrator)
	{
		_orchestrator = orchestrator;
		_orchestrator.WorldUpdated += OnWorldUpdated;
		_catalogQuoteSubscription = orchestrator.Subscribe<Record<MerchantCatalogQuote>>(
			record => MerchantCatalogQuoted?.Invoke(record.Value));
	}

	public event Action? WorldUpdated;
	public event Action<MerchantCatalogQuote>? MerchantCatalogQuoted;

	public State Run => Session.Instance.Run;

	public StarMap Map => _orchestrator.Map;

	public bool TryCommitPlayerInput(IAction action) => _orchestrator.TryCommitPlayerInput(action);

	public StarSystemOrchestrator StarSystem => _orchestrator;

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_orchestrator.WorldUpdated -= OnWorldUpdated;
		_catalogQuoteSubscription.Dispose();
		WorldUpdated = null;
		MerchantCatalogQuoted = null;
	}

	private void OnWorldUpdated() => WorldUpdated?.Invoke();
}
