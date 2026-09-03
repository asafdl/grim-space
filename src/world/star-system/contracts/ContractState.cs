namespace GrimSpace.World.StarSystem.Contracts;

public sealed record ContractState(
	string ContractId,
	EContractStatus Status,
	int? AcceptedAtTick,
	string? HolderUnitId,
	IReadOnlyDictionary<string, IReadOnlyList<string>> SpawnBindings)
{
	public static IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyBindings { get; } =
		new Dictionary<string, IReadOnlyList<string>>();
}
