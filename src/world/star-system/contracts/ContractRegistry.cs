namespace GrimSpace.World.StarSystem.Contracts;

public sealed class ContractRegistry
{
	private readonly Dictionary<string, Contract> _contracts = new(StringComparer.Ordinal);
	private readonly Dictionary<string, ContractState> _states = new(StringComparer.Ordinal);

	public IEnumerable<Contract> All => _contracts.Values;

	public IEnumerable<Contract> Offered =>
		_contracts.Values.Where(contract => !_states.ContainsKey(contract.Id));

	public bool Contains(string contractId) => _contracts.ContainsKey(contractId);

	public bool IsOffered(string contractId) =>
		_contracts.ContainsKey(contractId) && !_states.ContainsKey(contractId);

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

	public void RegisterOffered(Contract contract)
	{
		ArgumentNullException.ThrowIfNull(contract);
		if (_contracts.ContainsKey(contract.Id))
			throw new InvalidOperationException($"Contract '{contract.Id}' is already registered.");

		_contracts[contract.Id] = contract;
	}

	public void Accept(string contractId, string holderUnitId, int acceptedAtTick)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentException.ThrowIfNullOrEmpty(holderUnitId);

		if (!_contracts.ContainsKey(contractId))
			throw new InvalidOperationException($"Contract '{contractId}' does not exist.");

		if (_states.ContainsKey(contractId))
			throw new InvalidOperationException($"Contract '{contractId}' is not offered.");

		var state = new ContractState(
			contractId,
			EContractStatus.Active,
			acceptedAtTick,
			holderUnitId);
		_states[contractId] = state;
	}

	public ContractRegistry CloneForFork()
	{
		var clone = new ContractRegistry();
		foreach (var (id, contract) in _contracts)
			clone._contracts[id] = contract;
		foreach (var (id, state) in _states)
			clone._states[id] = state;
		return clone;
	}
}
