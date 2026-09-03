namespace GrimSpace.World.StarSystem.Contracts;

public sealed record ContractState(
	string ContractId,
	EContractStatus Status,
	int? AcceptedAtTick,
	string? HolderUnitId);
