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

public sealed record DeliveryProgress
{
	public IReadOnlyList<bool> CompletedLegs { get; init; }
	public int CurrentLegIndex { get; init; }
	public int? ActivationTick { get; init; }
	public int? DeadlineTick { get; init; }
	public bool RedirectAcknowledged { get; init; }
	public string? InterceptorFleetId { get; init; }
	public bool InterceptionTriggered { get; init; }
	public bool InterceptorResolved { get; init; }

	public DeliveryProgress(
		IReadOnlyList<bool> completedLegs,
		int currentLegIndex = 0,
		int? activationTick = null,
		int? deadlineTick = null,
		bool redirectAcknowledged = false,
		string? interceptorFleetId = null,
		bool interceptionTriggered = false,
		bool interceptorResolved = false)
	{
		ArgumentNullException.ThrowIfNull(completedLegs);
		if (completedLegs.Count == 0)
			throw new ArgumentException("Delivery progress must contain at least one leg.", nameof(completedLegs));
		if (currentLegIndex < 0 || currentLegIndex >= completedLegs.Count)
			throw new ArgumentOutOfRangeException(nameof(CurrentLegIndex));
		CompletedLegs = completedLegs;
		CurrentLegIndex = currentLegIndex;
		ActivationTick = activationTick;
		DeadlineTick = deadlineTick;
		RedirectAcknowledged = redirectAcknowledged;
		InterceptorFleetId = interceptorFleetId;
		InterceptionTriggered = interceptionTriggered;
		InterceptorResolved = interceptorResolved;
	}

	public bool IsComplete => CompletedLegs.All(completed => completed);

	public DeliveryProgress MarkLegCompleted(int legIndex)
	{
		if (legIndex < 0 || legIndex >= CompletedLegs.Count || CompletedLegs[legIndex])
			return this;

		var completed = CompletedLegs.ToArray();
		completed[legIndex] = true;
		var nextLeg = Array.FindIndex(completed, isComplete => !isComplete);
		return this with
		{
			CompletedLegs = completed,
			CurrentLegIndex = nextLeg < 0 ? completed.Length - 1 : nextLeg,
		};
	}
}

public sealed record DeliveryContractState(
	string ContractId,
	EContractStatus Status,
	int? AcceptedAtTick,
	string? HolderUnitId,
	DeliveryProgress Progress)
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
			new DeliveryProgress(Enumerable.Repeat(false, legCount).ToArray()))
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(legCount, 1);
	}

	public IReadOnlyList<bool> DeliveryProgressLegs => Progress.CompletedLegs;

	public override bool IsObjectiveMet() =>
		Progress.IsComplete;

	public DeliveryContractState MarkLegCompleted(int legIndex)
	{
		return this with { Progress = Progress.MarkLegCompleted(legIndex) };
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
