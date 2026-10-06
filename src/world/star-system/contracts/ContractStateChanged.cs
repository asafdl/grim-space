namespace GrimSpace.World.StarSystem.Contracts;

public sealed record ContractStateChanged(
	string ContractId,
	string HolderUnitId,
	EContractStatus Status);
