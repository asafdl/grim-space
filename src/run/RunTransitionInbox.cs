using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run;

public sealed class RunTransitionInbox : IDisposable
{
	private readonly Queue<Transaction> _resourceTransactions = [];
	private StarSystemOrchestrator? _orchestrator;

	public event Action? ResourceTransactionsAvailable;

	public void Bind(StarSystemOrchestrator orchestrator)
	{
		ArgumentNullException.ThrowIfNull(orchestrator);

		if (_orchestrator is not null)
			_orchestrator.ResourceTransactionCommitted -= OnResourceTransactionCommitted;

		_resourceTransactions.Clear();
		_orchestrator = orchestrator;
		_orchestrator.ResourceTransactionCommitted += OnResourceTransactionCommitted;
	}

	public IReadOnlyList<Transaction> DrainResourceTransactions()
	{
		if (_resourceTransactions.Count == 0)
			return [];

		var transactions = _resourceTransactions.ToArray();
		_resourceTransactions.Clear();
		return transactions;
	}

	public void Dispose()
	{
		if (_orchestrator is not null)
			_orchestrator.ResourceTransactionCommitted -= OnResourceTransactionCommitted;

		_orchestrator = null;
		_resourceTransactions.Clear();
		ResourceTransactionsAvailable = null;
	}

	private void OnResourceTransactionCommitted(Transaction transaction)
	{
		_resourceTransactions.Enqueue(transaction);
		ResourceTransactionsAvailable?.Invoke();
	}
}
