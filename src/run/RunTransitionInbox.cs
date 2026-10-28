using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run;

public sealed class RunTransitionInbox : IDisposable
{
	/// <summary>
	/// Matches <c>ResourceHud</c> credit count-up speed so battle entry holds through the reveal.
	/// </summary>
	private const float ResourceCountUpSpeed = 140f;
	private const float HazardPayRevealBeatSeconds = 0.55f;
	private const float HazardPayRevealMinSeconds = 0.75f;
	private const float HazardPayRevealMaxSeconds = 2.0f;

	private readonly Queue<Transaction> _resourceTransactions = [];
	private StarSystemOrchestrator? _orchestrator;
	private float _battleEntryHoldSeconds;

	public event Action? ResourceTransactionsAvailable;

	public void Bind(StarSystemOrchestrator orchestrator)
	{
		ArgumentNullException.ThrowIfNull(orchestrator);

		if (_orchestrator is not null)
			_orchestrator.ResourceTransactionCommitted -= OnResourceTransactionCommitted;

		_resourceTransactions.Clear();
		_battleEntryHoldSeconds = 0f;
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

	public float ConsumeBattleEntryHoldSeconds()
	{
		var hold = _battleEntryHoldSeconds;
		_battleEntryHoldSeconds = 0f;
		return hold;
	}

	public void Dispose()
	{
		if (_orchestrator is not null)
			_orchestrator.ResourceTransactionCommitted -= OnResourceTransactionCommitted;

		_orchestrator = null;
		_resourceTransactions.Clear();
		_battleEntryHoldSeconds = 0f;
		ResourceTransactionsAvailable = null;
	}

	private void OnResourceTransactionCommitted(Transaction transaction)
	{
		_resourceTransactions.Enqueue(transaction);
		if (transaction.Source == TransactionSource.HazardPay)
			_battleEntryHoldSeconds = System.Math.Max(
				_battleEntryHoldSeconds,
				EstimateHazardPayRevealSeconds(transaction));
		ResourceTransactionsAvailable?.Invoke();
	}

	private static float EstimateHazardPayRevealSeconds(Transaction transaction)
	{
		if (!transaction.Change.TryGet(ResourceId.Credits, out var creditsDelta))
			return HazardPayRevealMinSeconds;

		var countSeconds = System.Math.Abs(creditsDelta) / ResourceCountUpSpeed;
		return System.Math.Clamp(
			countSeconds + HazardPayRevealBeatSeconds,
			HazardPayRevealMinSeconds,
			HazardPayRevealMaxSeconds);
	}
}
