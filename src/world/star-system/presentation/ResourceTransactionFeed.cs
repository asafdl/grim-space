using GrimSpace.Run;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Presentation;

public sealed class ResourceTransactionFeed : IDisposable
{
	private RunTransitionInbox? _inbox;
	private ResourceHud? _hud;

	public void Bind(
		RunTransitionInbox inbox,
		StarSystemOrchestrator orchestrator,
		ResourceHud hud)
	{
		ArgumentNullException.ThrowIfNull(inbox);
		ArgumentNullException.ThrowIfNull(orchestrator);
		ArgumentNullException.ThrowIfNull(hud);

		_inbox = inbox;
		_hud = hud;
		_inbox.ResourceTransactionsAvailable += PresentPendingTransactions;

		var pending = _inbox.DrainResourceTransactions();
		var balances = orchestrator.Map.PlayerResources.EnumerateBalances().ToDictionary(
			entry => entry.Id,
			entry => entry.Balance);
		foreach (var transaction in pending)
		{
			foreach (var (id, delta) in transaction.Change)
				balances[id] = checked(balances.GetValueOrDefault(id) - delta);
		}

		hud.SetBalances(balances);
		foreach (var transaction in pending)
			hud.PresentTransaction(transaction);
	}

	public void Dispose()
	{
		if (_inbox is not null)
			_inbox.ResourceTransactionsAvailable -= PresentPendingTransactions;

		_inbox = null;
		_hud = null;
	}

	private void PresentPendingTransactions()
	{
		if (_inbox is null || _hud is null)
			return;

		foreach (var transaction in _inbox.DrainResourceTransactions())
			_hud.PresentTransaction(transaction);
	}
}
