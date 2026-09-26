using GrimSpace.Core.Actions;
using GrimSpace.Core.Ids;
using GrimSpace.Math;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Encounter;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractFactory
{
	public static Contract Create(StarMap map, EContractKind kind, ContractCreateArgs args)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(args);

		var contract = Build(map, TypedIdGenerator.NextId("contract"), kind, args);
		if (!map.ContractRegistry.TryAdd(contract))
			throw new InvalidOperationException($"Failed to add contract '{contract.Id}'.");
		return contract;
	}

	public static Contract Build(StarMap map, string contractId, EContractKind kind, ContractCreateArgs args)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentNullException.ThrowIfNull(args);

		return kind switch
		{
			EContractKind.Hunt => args switch
			{
				HuntCreateArgs huntArgs => BuildHunt(map, contractId, huntArgs),
				_ => throw Mismatch(kind, args),
			},
			EContractKind.Delivery => args switch
			{
				DeliveryCreateArgs deliveryArgs => BuildDelivery(map, contractId, deliveryArgs),
				_ => throw Mismatch(kind, args),
			},
			EContractKind.Wreckage => args switch
			{
				WreckageCreateArgs wreckageArgs => BuildWreckage(map, contractId, wreckageArgs),
				_ => throw Mismatch(kind, args),
			},
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};
	}

	private static ArgumentException Mismatch(EContractKind kind, ContractCreateArgs args) =>
		new($"Contract kind '{kind}' requires matching create args, but received '{args.GetType().Name}'.", nameof(args));

	private static Contract BuildHunt(StarMap map, string contractId, HuntCreateArgs args)
	{
		ArgumentException.ThrowIfNullOrEmpty(args.IssuerPoiId);
		ArgumentNullException.ThrowIfNull(args.SearchAreaPicker);

		var faction = ResolveOppositionFaction(map, contractId);
		var fleetType = ResolveFleetType(faction);
		var objective = CreateHuntObjective(
			map,
			contractId,
			args.SearchAreaPicker,
			args.Danger,
			fleetType,
			faction);

		return new Contract(
			contractId,
			objective,
			args.Danger,
			map.ControllingFaction,
			args.IssuerPoiId,
			ResolvePayment(map.Seed, contractId, EContractKind.Hunt, args.Danger),
			args.Narrative,
			IsHuntObjectiveMet,
			args.IsStoryObjective);
	}

	internal static HuntObjective CreateHuntObjective(
		StarMap map,
		string contractId,
		AreaPickerArgs searchAreaPicker,
		EDangerLevel danger,
		FleetType fleetType,
		EFaction faction)
	{
		var groupId = SpawnGroupIdFor(contractId);
		var spawnSeeds = CreateSpawnSeeds(map.Seed, contractId, groupId, 1);
		if (!TryPickSearchArea(map, searchAreaPicker, spawnSeeds, out var searchArea))
		{
			throw new InvalidOperationException(
				$"Could not pick a hunt search area for map seed {map.Seed}.");
		}

		var spawnSeed = unchecked((int)StableSeedMixer.From(map.Seed).Add(contractId).Add(groupId).Value);
		var members = EncounterBudgetRoller.Roll(map.Seed, contractId, "hunt-encounter", danger);
		var spawnSpec = new FleetSpawnSpec(fleetType, faction, spawnSeed, members);
		return new HuntObjective(
		[
			new SpawnEncounterGroup(groupId, searchArea, 1, spawnSpec),
		]);
	}

	private static Contract BuildDelivery(StarMap map, string contractId, DeliveryCreateArgs args)
	{
		ArgumentException.ThrowIfNullOrEmpty(args.IssuerPoiId);

		var (turnInPoiId, turnInFacilityId, turnInOperatorName) = ResolveDropoff(map, args, contractId);
		var objective = new DeliveryObjective(turnInPoiId, turnInFacilityId, turnInOperatorName);

		return new Contract(
			contractId,
			objective,
			args.Danger,
			map.ControllingFaction,
			args.IssuerPoiId,
			ResolvePayment(map.Seed, contractId, EContractKind.Delivery, args.Danger),
			args.Narrative,
			IsDeliveryObjectiveMet,
			args.IsStoryObjective);
	}

	public static bool TryBuildWreckage(
		StarMap map,
		string contractId,
		WreckageCreateArgs args,
		out Contract contract)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentNullException.ThrowIfNull(args);
		contract = null!;

		ArgumentException.ThrowIfNullOrEmpty(args.IssuerPoiId);
		ArgumentNullException.ThrowIfNull(args.SearchAreaPicker);
		var wreckageId = WreckageIdFor(contractId);
		var spawnSeeds = CreateSpawnSeeds(map.Seed, contractId, wreckageId, 1);
		if (args.SearchAreaPicker.LandmarkCandidateIds.Count < 1
			|| !TryPickSearchArea(map, args.SearchAreaPicker, spawnSeeds, out var searchArea))
			return false;

		var outcome = RollWreckageOutcome(map, contractId, args);
		var objective = new WreckageObjective(wreckageId, searchArea, outcome);
		contract = new Contract(
			contractId,
			objective,
			args.Danger,
			map.ControllingFaction,
			args.IssuerPoiId,
			ResolvePayment(map.Seed, contractId, EContractKind.Wreckage, args.Danger),
			args.Narrative,
			IsWreckageObjectiveMet,
			args.IsStoryObjective);
		return true;
	}

	private static Contract BuildWreckage(StarMap map, string contractId, WreckageCreateArgs args)
	{
		if (!TryBuildWreckage(map, contractId, args, out var contract))
		{
			throw new InvalidOperationException(
				$"Could not build wreckage contract for map seed {map.Seed}.");
		}

		return contract;
	}

	private static WreckageOutcome RollWreckageOutcome(StarMap map, string contractId, WreckageCreateArgs args)
	{
		if (WreckageSalvageRoller.RollSalvageOutcome(map.Seed, contractId))
			return new WreckageOutcome.Salvage(
				WreckageSalvageRoller.RollSalvageLoot(map.Seed, contractId, args.Danger));

		var faction = ResolveOppositionFaction(map, contractId);
		var fleetType = ResolveFleetType(faction);
		var ambushSeed = unchecked((int)StableSeedMixer.From(map.Seed)
			.Add(contractId)
			.Add("wreckage-ambush")
			.Value);
		var members = EncounterBudgetRoller.Roll(map.Seed, contractId, "wreckage-ambush", args.Danger);
		return new WreckageOutcome.Ambush(
			new FleetSpawnSpec(fleetType, faction, ambushSeed, members));
	}

	private static EFaction ResolveOppositionFaction(StarMap map, string contractId)
	{
		_ = map;
		_ = contractId;
		return EFaction.Pirates;
	}

	private static FleetType ResolveFleetType(EFaction faction) =>
		faction switch
		{
			EFaction.Pirates => FleetType.PirateFleet,
			_ => throw new ArgumentOutOfRangeException(nameof(faction), faction, "Unsupported opposition faction."),
		};

	private static ContractTerms ResolvePayment(
		int mapSeed,
		string contractId,
		EContractKind kind,
		EDangerLevel danger) =>
		ContractRewardCalculator.Roll(mapSeed, contractId, kind, danger);

	private static bool TryPickSearchArea(
		StarMap map,
		AreaPickerArgs args,
		IReadOnlyList<ulong> spawnSeeds,
		out AreaPick pick)
	{
		if (AreaPicker.TryPick(map, args, spawnSeeds, out pick))
			return true;

		var alternate = args.ReferenceMode == EAreaPickerReferenceMode.LandmarkWithBorderTriangle
			? EAreaPickerReferenceMode.TriangulateLandmarks
			: EAreaPickerReferenceMode.LandmarkWithBorderTriangle;
		if (alternate == EAreaPickerReferenceMode.TriangulateLandmarks
			&& args.LandmarkCandidateIds.Count < 3)
			return false;

		return AreaPicker.TryPick(map, args with { ReferenceMode = alternate }, spawnSeeds, out pick);
	}

	internal static ulong[] CreateSpawnSeeds(int mapSeed, string contractId, string scopeId, int count)
	{
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 0);
		var seeds = new ulong[count];
		for (var index = 0; index < count; index++)
		{
			seeds[index] = StableSeedMixer.From(mapSeed)
				.Add(contractId)
				.Add(scopeId)
				.Add(index)
				.Value;
		}

		return seeds;
	}

	internal static string SpawnGroupIdFor(string contractId) => $"{contractId}.spawns";

	internal static string WreckageIdFor(string contractId) => $"{contractId}.wreckage";

	private static (string PoiId, string FacilityId, string OperatorName) ResolveDropoff(
		StarMap map,
		DeliveryCreateArgs args,
		string contractId)
	{
		var hasOverride = args.DropoffPoiId is not null
			|| args.DropoffFacilityId is not null
			|| args.DropoffOperatorName is not null;
		if (!hasOverride)
			return DeliveryDropoffPicker.Pick(map, args.IssuerPoiId, contractId);

		if (string.IsNullOrEmpty(args.DropoffPoiId)
			|| string.IsNullOrEmpty(args.DropoffFacilityId)
			|| string.IsNullOrEmpty(args.DropoffOperatorName))
			throw new ArgumentException(
				"Delivery dropoff override requires PoiId, FacilityId, and OperatorName.",
				nameof(args));

		return (args.DropoffPoiId, args.DropoffFacilityId, args.DropoffOperatorName);
	}

	internal static bool IsHuntObjectiveMet(string contractId, StarMap map, string actorId)
	{
		_ = actorId;
		if (!map.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not HuntObjective hunt)
			return false;

		foreach (var group in hunt.SpawnGroups)
		{
			for (var index = 0; index < group.RequiredCount; index++)
			{
				var unitId = $"{contractId}.{group.GroupId}.{index}";
				if (map.FleetRegistry.Contains(unitId))
					return false;
			}
		}

		return true;
	}

	internal static bool IsDeliveryObjectiveMet(string contractId, StarMap map, string actorId)
	{
		_ = actorId;
		return map.Timeline.History()
			.OfType<Record<DeliveryTurnedIn>>()
			.Any(record => string.Equals(record.Value.contractId, contractId, StringComparison.Ordinal));
	}

	internal static bool IsWreckageObjectiveMet(string contractId, StarMap map, string actorId)
	{
		_ = actorId;
		if (!map.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not WreckageObjective wreckage)
			return false;

		return map.Timeline.History()
			.OfType<Record<WreckInvestigated>>()
			.Any(record =>
				string.Equals(record.Value.contractId, contractId, StringComparison.Ordinal)
				&& string.Equals(record.Value.wreckageId, wreckage.WreckageId, StringComparison.Ordinal));
	}

}
