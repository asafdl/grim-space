using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts;

public record ContractState(
	string ContractId,
	EContractStatus Status,
	int? AcceptedAtTick,
	string? HolderUnitId)
{
	public static ContractState CreateFor(
		Contract contract,
		EContractStatus status,
		int? acceptedAtTick,
		string? holderUnitId,
		IReadOnlyList<string>? spawnedFleetIds = null) =>
		contract.Objective switch
		{
			HuntObjective => new HuntContractState(
				contract.Id,
				status,
				acceptedAtTick,
				holderUnitId,
				spawnedFleetIds ?? [],
				(spawnedFleetIds ?? []).Select(_ => false).ToArray()),
			DeliveryObjective delivery => new DeliveryContractState(
				contract.Id,
				status,
				acceptedAtTick,
				holderUnitId,
				delivery.RouteLegCount),
			WreckageObjective => new WreckageContractState(
				contract.Id,
				status,
				acceptedAtTick,
				holderUnitId,
				false),
			_ => throw new ArgumentOutOfRangeException(
				nameof(contract),
				contract.Objective,
				$"Unsupported contract objective '{contract.Objective.GetType().Name}'."),
		};

	public virtual bool IsObjectiveMet() =>
		throw new InvalidOperationException(
			$"Contract state '{GetType().Name}' does not implement objective completion.");
}

public sealed record HuntContractState(
	string ContractId,
	EContractStatus Status,
	int? AcceptedAtTick,
	string? HolderUnitId,
	IReadOnlyList<string> SpawnedFleetIds,
	IReadOnlyList<bool> SpawnProgress)
	: ContractState(ContractId, Status, AcceptedAtTick, HolderUnitId)
{
	public override bool IsObjectiveMet() => SpawnProgress.All(completed => completed);

	public HuntContractState MarkFleetDefeated(string fleetId)
	{
		var index = -1;
		for (var candidate = 0; candidate < SpawnedFleetIds.Count; candidate++)
		{
			if (string.Equals(SpawnedFleetIds[candidate], fleetId, StringComparison.Ordinal))
			{
				index = candidate;
				break;
			}
		}

		if (index < 0 || SpawnProgress[index])
			return this;

		var progress = SpawnProgress.ToArray();
		progress[index] = true;
		return this with { SpawnProgress = progress };
	}
}

public sealed record DeliveryContractState(
	string ContractId,
	EContractStatus Status,
	int? AcceptedAtTick,
	string? HolderUnitId,
	IReadOnlyList<bool> DeliveryProgressLegs)
	: ContractState(ContractId, Status, AcceptedAtTick, HolderUnitId)
{
	public DeliveryContractState(
		string contractId,
		EContractStatus status,
		int? acceptedAtTick,
		string? holderUnitId,
		int legCount)
		: this(
			contractId,
			status,
			acceptedAtTick,
			holderUnitId,
			Enumerable.Repeat(false, legCount).ToArray())
	{
	}

	public override bool IsObjectiveMet() =>
		DeliveryProgressLegs.All(completed => completed);

	public DeliveryContractState MarkLegCompleted(int legIndex)
	{
		if (legIndex < 0 || legIndex >= DeliveryProgressLegs.Count || DeliveryProgressLegs[legIndex])
			return this;

		var progress = DeliveryProgressLegs.ToArray();
		progress[legIndex] = true;
		return this with { DeliveryProgressLegs = progress };
	}
}

public sealed record WreckageContractState(
	string ContractId,
	EContractStatus Status,
	int? AcceptedAtTick,
	string? HolderUnitId,
	bool Investigated)
	: ContractState(ContractId, Status, AcceptedAtTick, HolderUnitId)
{
	public override bool IsObjectiveMet() => Investigated;
}
