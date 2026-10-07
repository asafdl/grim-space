using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Units;

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
	public EDeliveryInterceptionState InterceptionState { get; init; }
	public EDeliveryFailureReason? FailureReason { get; init; }

	public DeliveryProgress(
		IReadOnlyList<bool> completedLegs,
		int currentLegIndex = 0,
		int? activationTick = null,
		int? deadlineTick = null,
		bool redirectAcknowledged = false,
		string? interceptorFleetId = null,
		EDeliveryInterceptionState interceptionState = EDeliveryInterceptionState.None,
		EDeliveryFailureReason? failureReason = null)
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
		InterceptionState = interceptionState;
		FailureReason = failureReason;
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

	public DeliveryContractState WithDeadlineTick(int? deadlineTick) =>
		this with { Progress = Progress with { DeadlineTick = deadlineTick } };

	public DeliveryContractState WithInterceptorAssigned(string interceptorFleetId) =>
		this with
		{
			Progress = Progress with
			{
				InterceptionState = EDeliveryInterceptionState.Assigned,
				InterceptorFleetId = interceptorFleetId,
			},
		};

	public DeliveryContractState WithInterceptorResolved(string interceptorFleetId)
	{
		if (!string.Equals(
				Progress.InterceptorFleetId,
				interceptorFleetId,
				StringComparison.Ordinal))
			return this;

		return this with
		{
			Progress = Progress with
			{
				InterceptionState = EDeliveryInterceptionState.Resolved,
			},
		};
	}

	public DeliveryContractState WithFailureReason(EDeliveryFailureReason reason) =>
		this with { Progress = Progress with { FailureReason = reason } };

	public static DeliveryContractState CreateActiveOnAccept(
		StarMap world,
		Contract contract,
		DeliveryObjective delivery,
		string holderUnitId,
		int acceptedAtTick)
	{
		var config = delivery.Config;
		var issuerPoiId = contract.IssuerPoiId
			?? throw new InvalidOperationException(
				$"Delivery contract '{contract.Id}' has no issuer POI.");
		var origin = world.GetPointOfInterest(issuerPoiId).PlacedCenter;
		int? deadlineTick = ResolveDeadlineTickForLeg(
			world,
			contract.Id,
			delivery,
			legIndex: 0,
			origin,
			acceptedAtTick,
			TimedLegChanceFor(contract, config));
		var interceptionState = contract.IsStoryObjective
			? EDeliveryInterceptionState.None
			: RollInterception(world.Seed, contract.Id, contract.Danger, config)
				? EDeliveryInterceptionState.Pending
				: EDeliveryInterceptionState.None;
		var progress = new DeliveryProgress(
			Enumerable.Repeat(false, delivery.RouteLegCount).ToArray(),
			activationTick: acceptedAtTick,
			deadlineTick: deadlineTick,
			interceptionState: interceptionState);
		return new DeliveryContractState(
			contract.Id,
			EContractStatus.Active,
			acceptedAtTick,
			holderUnitId,
			progress);
	}

	internal static double TimedLegChanceFor(Contract contract, DeliveryGenerationConfig config) =>
		contract.IsStoryObjective ? 0 : config.TimedLegChance;

	internal static int? ResolveDeadlineTickForLeg(
		StarMap world,
		string contractId,
		DeliveryObjective delivery,
		int legIndex,
		Coord origin,
		int startsAtTick,
		double timedLegChance) =>
		RollTimedLeg(world.Seed, contractId, legIndex, timedLegChance)
			? DeadlineTickForLeg(world, delivery, legIndex, origin, startsAtTick)
			: null;

	internal static int DeadlineTickForLeg(
		StarMap world,
		DeliveryObjective delivery,
		int legIndex,
		Coord origin,
		int startsAtTick)
	{
		var travelTicks = EstimateLegTravelTicks(world, delivery.Route, legIndex, origin);
		return startsAtTick
			+ (int)System.Math.Ceiling(
				travelTicks * delivery.Config.DeadlineSlackMultiplier)
			+ delivery.Config.InterruptionBufferTicks;
	}

	internal static bool RollTimedLeg(
		int mapSeed,
		string contractId,
		int legIndex,
		DeliveryGenerationConfig config) =>
		RollTimedLeg(mapSeed, contractId, legIndex, config.TimedLegChance);

	internal static bool RollTimedLeg(
		int mapSeed,
		string contractId,
		int legIndex,
		double timedLegChance)
	{
		if (timedLegChance <= 0)
			return false;

		var random = new StableRandom(
			StableSeedMixer.From(mapSeed)
				.Add(contractId)
				.Add("delivery-timed-leg-roll")
				.Add(legIndex)
				.Value);
		return random.NextDouble() < timedLegChance;
	}

	internal static double EstimateLegTravelTicks(
		StarMap world,
		DeliveryRoute route,
		int legIndex,
		Coord origin)
	{
		if (legIndex < 0 || legIndex >= route.Legs.Count)
			throw new ArgumentOutOfRangeException(nameof(legIndex));

		var destination = CoordinateOf(world, route.Legs[legIndex]);
		var speed = UnitDefaults.SpeedPerTick(EType.PlayerFleet);
		return origin.ManhattanDistanceTo(destination) / speed;
	}

	internal static bool RollInterception(
		int mapSeed,
		string contractId,
		EDangerLevel danger,
		DeliveryGenerationConfig config)
	{
		var chance = config.InterceptionChanceFor(danger);
		if (chance <= 0)
			return false;

		var random = new StableRandom(
			StableSeedMixer.From(mapSeed).Add(contractId).Add("delivery-interception-roll").Value);
		return random.NextDouble() < chance;
	}

	internal static Coord CoordinateOf(StarMap world, DeliveryLeg leg) =>
		leg switch
		{
			FacilityDeliveryLeg facility => world.GetPointOfInterest(facility.PoiId).PlacedCenter,
			SpaceMeetingDeliveryLeg meeting => meeting.Position,
			_ => throw new InvalidOperationException(
				$"Unsupported delivery leg type '{leg.GetType().Name}'."),
		};
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
