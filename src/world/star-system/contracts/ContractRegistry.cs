namespace GrimSpace.World.StarSystem.Contracts;

public sealed class ContractRegistry
{
	private readonly Dictionary<string, Contract> _contracts = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ContractState> _states = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int?> _expirations = new(StringComparer.Ordinal);
	private int _pendingCount;
	private int _pendingGeneratedCount;

	public int MaxPending { get; set; } = int.MaxValue;

	public IEnumerable<Contract> All => _contracts.Values;

	public IEnumerable<Contract> Pending =>
		_contracts.Values.Where(contract => !_states.ContainsKey(contract.Id));

	public int CountPending() => _pendingCount;

	public int CountPendingGenerated() => _pendingGeneratedCount;

	public IEnumerable<Contract> AvailableForPoi(string poiId) =>
		Pending.Where(contract => contract.IssuerPoiId == poiId);

	public bool Contains(string contractId) => _contracts.ContainsKey(contractId);

	public bool IsPending(string contractId) =>
		_contracts.ContainsKey(contractId) && !_states.ContainsKey(contractId);

	public bool IsRejected(string contractId) =>
		_states.TryGetValue(contractId, out var state) && state.Status == EContractStatus.Rejected;

	public bool TryGet(string contractId, out Contract contract) =>
		_contracts.TryGetValue(contractId, out contract!);

	public bool TryGetState(string contractId, out ContractState state) =>
		_states.TryGetValue(contractId, out state!);

	public bool TryGetActive(string unitId, out ActiveContract active)
	{
		foreach (var state in _states.Values)
		{
			if (state.Status != EContractStatus.Active || state.HolderUnitId != unitId)
				continue;

			active = new ActiveContract(_contracts[state.ContractId], state);
			return true;
		}

		active = null!;
		return false;
	}

	public IEnumerable<ActiveContract> ActiveFor(string unitId) =>
		_states.Values
			.Where(state => state.Status == EContractStatus.Active && state.HolderUnitId == unitId)
			.Select(state => new ActiveContract(_contracts[state.ContractId], state));

	public bool IsCompleted(string contractId) =>
		_states.TryGetValue(contractId, out var state) && state.Status == EContractStatus.Completed;

	public int CountCompleted() =>
		_states.Values.Count(state => state.Status == EContractStatus.Completed);

	public void Complete(string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);

		if (!_states.TryGetValue(contractId, out var state))
			throw new InvalidOperationException($"Contract '{contractId}' has no runtime state.");

		if (state.Status != EContractStatus.Active)
			throw new InvalidOperationException($"Contract '{contractId}' is not active.");

		_states[contractId] = state with { Status = EContractStatus.Completed };
	}

	public bool TryAdd(Contract contract, int? expiresAtTick = null)
	{
		ArgumentNullException.ThrowIfNull(contract);
		if (_contracts.ContainsKey(contract.Id))
			throw new InvalidOperationException($"Contract '{contract.Id}' is already registered.");

		if (_pendingCount >= MaxPending)
			return false;

		_contracts[contract.Id] = contract;
		_expirations[contract.Id] = expiresAtTick;
		_pendingCount++;
		if (!contract.IsStoryObjective)
			_pendingGeneratedCount++;
		return true;
	}

	public bool TryGetExpiration(string contractId, out int? expiresAtTick) =>
		_expirations.TryGetValue(contractId, out expiresAtTick);

	public IReadOnlyList<string> RemoveExpired(int currentTick)
	{
		var removed = new List<string>();
		foreach (var contract in Pending.ToList())
		{
			if (!_expirations.TryGetValue(contract.Id, out var expiresAtTick) || expiresAtTick is not int tick)
				continue;

			if (tick > currentTick)
				continue;

			removed.Add(contract.Id);
			Remove(contract.Id);
		}

		return removed;
	}

	public void Remove(string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		if (!_contracts.TryGetValue(contractId, out var contract))
			throw new InvalidOperationException($"Contract '{contractId}' is not registered.");

		var wasPending = !_states.ContainsKey(contractId);
		_contracts.Remove(contractId);
		_expirations.Remove(contractId);
		if (!wasPending)
			return;

		_pendingCount--;
		if (!contract.IsStoryObjective)
			_pendingGeneratedCount--;
	}

	public bool Activate(ContractState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		ArgumentException.ThrowIfNullOrEmpty(state.ContractId);

		if (state.Status == EContractStatus.Active && string.IsNullOrEmpty(state.HolderUnitId))
			return false;

		if (!_contracts.ContainsKey(state.ContractId))
			return false;

		if (_states.ContainsKey(state.ContractId))
			return false;

		_states[state.ContractId] = state;
		OnLeftPending(_contracts[state.ContractId]);
		return true;
	}

	public void Deactivate(string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);

		if (!_states.Remove(contractId))
			throw new InvalidOperationException($"Contract '{contractId}' is not active.");

		if (_contracts.TryGetValue(contractId, out var contract))
			OnBecamePending(contract);
	}

	internal void Restore(ContractState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		if (!_contracts.TryGetValue(state.ContractId, out var contract))
			throw new InvalidOperationException($"Contract '{state.ContractId}' does not exist.");

		var wasPending = !_states.ContainsKey(state.ContractId);
		_states[state.ContractId] = state;
		if (wasPending)
			OnLeftPending(contract);
	}

	public ContractRegistry CloneForFork()
	{
		var clone = new ContractRegistry
		{
			MaxPending = MaxPending,
			_pendingCount = _pendingCount,
			_pendingGeneratedCount = _pendingGeneratedCount,
		};
		foreach (var (id, contract) in _contracts)
			clone._contracts[id] = contract;
		foreach (var (id, state) in _states)
			clone._states[id] = state;
		foreach (var (id, expiresAtTick) in _expirations)
			clone._expirations[id] = expiresAtTick;
		return clone;
	}

	private void OnBecamePending(Contract contract)
	{
		_pendingCount++;
		if (!contract.IsStoryObjective)
			_pendingGeneratedCount++;
	}

	private void OnLeftPending(Contract contract)
	{
		_pendingCount--;
		if (!contract.IsStoryObjective)
			_pendingGeneratedCount--;
	}
}
