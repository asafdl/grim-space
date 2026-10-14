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
	private readonly Action _presentInsufficientFunds;
	private readonly Action<string, string> _presentSystemError;
	private bool _disposed;

	public MapSceneBinding(
		StarSystemOrchestrator orchestrator,
		Action presentInsufficientFunds,
		Action<string, string> presentSystemError)
	{
		ArgumentNullException.ThrowIfNull(orchestrator);
		ArgumentNullException.ThrowIfNull(presentInsufficientFunds);
		ArgumentNullException.ThrowIfNull(presentSystemError);
		_orchestrator = orchestrator;
		_presentInsufficientFunds = presentInsufficientFunds;
		_presentSystemError = presentSystemError;
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

	public void PresentInsufficientFunds() => _presentInsufficientFunds();

	public void PresentSystemError(string summary, string technicalDetails) =>
		_presentSystemError(summary, technicalDetails);

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
