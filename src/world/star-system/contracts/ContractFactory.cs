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
using GrimSpace.World.StarSystem.Landmarks;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractFactory
{
	private const int HuntPatrolRadius = 24;

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
		if (TryBuildHunt(map, contractId, args, out var contract))
			return contract;

		throw new InvalidOperationException(
			$"Could not pick a hunt search area for map seed {map.Seed}.");
	}

	public static bool TryBuildHunt(
		StarMap map,
		string contractId,
		HuntCreateArgs args,
		out Contract contract)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentNullException.ThrowIfNull(args);
		ArgumentNullException.ThrowIfNull(args.SearchAreaPicker);

		var faction = ResolveOppositionFaction(map, contractId);
		var fleetType = ResolveFleetType(faction);
		if (!TryCreateHuntObjective(
			map,
			contractId,
			args.SearchAreaPicker,
			args.Danger,
			fleetType,
			faction,
			out var objective))
		{
			contract = null!;
			return false;
		}

		contract = new Contract(
			contractId,
			objective,
			args.Danger,
			map.ControllingFaction,
			ResolvePayment(map.Seed, contractId, EContractKind.Hunt, args.Danger),
			args.Narrative,
			args.IsStoryObjective);
		return true;
	}

	internal static HuntObjective CreateHuntObjective(
		StarMap map,
		string contractId,
		AreaPickerArgs searchAreaPicker,
		EDangerLevel danger,
		FleetType fleetType,
		EFaction faction)
	{
		if (TryCreateHuntObjective(
			map,
			contractId,
			searchAreaPicker,
			danger,
			fleetType,
			faction,
			out var objective))
			return objective;

		throw new InvalidOperationException(
			$"Could not pick a hunt search area for map seed {map.Seed}.");
	}

	private static bool TryCreateHuntObjective(
		StarMap map,
		string contractId,
		AreaPickerArgs searchAreaPicker,
		EDangerLevel danger,
		FleetType fleetType,
		EFaction faction,
		out HuntObjective objective)
	{
		var groupId = SpawnGroupIdFor(contractId);
		var spawnSeeds = CreateSpawnSeeds(map.Seed, contractId, groupId, 1);
		if (!AreaPicker.TryPickWithFallback(map, searchAreaPicker, spawnSeeds, out var searchArea))
		{
			objective = null!;
			return false;
		}

		var spawnSeed = unchecked((int)StableSeedMixer.From(map.Seed).Add(contractId).Add(groupId).Value);
		var members = EncounterBudgetRoller.Roll(map.Seed, contractId, "hunt-encounter", danger);
		var spawnSpec = new FleetSpawnSpec(
			fleetType,
			faction,
			spawnSeed,
			members,
			HuntPatrolRadius);
		objective = new HuntObjective(
		[
			new SpawnEncounterGroup(groupId, searchArea, 1, spawnSpec),
		]);
		return true;
	}

	private static Contract BuildDelivery(StarMap map, string contractId, DeliveryCreateArgs args)
	{
		ArgumentException.ThrowIfNullOrEmpty(args.PickupPoiId);

		var config = args.Generation ?? DeliveryGenerationConfig.Default;
		var objective = new DeliveryObjective(
			args.PickupPoiId,
			ResolveDeliveryRoute(map, args, contractId),
			config);

		return new Contract(
			contractId,
			objective,
			args.Danger,
			map.ControllingFaction,
			ResolvePayment(
				map.Seed,
				contractId,
				EContractKind.Delivery,
				args.Danger),
			args.Narrative,
			args.IsStoryObjective);
	}

	private static DeliveryRoute ResolveDeliveryRoute(
		StarMap map,
		DeliveryCreateArgs args,
		string contractId)
	{
		var hasOverride = args.DropoffPoiId is not null
			|| args.DropoffFacilityId is not null
			|| args.DropoffOperatorName is not null;
		if (!hasOverride)
		{
			var legCount = args.Generation?.FacilityLegCount ?? 1;
			var legs = new List<DeliveryLeg>(legCount);
			var excludedPoiIds = new HashSet<string>(StringComparer.Ordinal);
			for (var legIndex = 0; legIndex < legCount; legIndex++)
			{
				if (legIndex < legCount - 1
					&& ShouldGenerateSpaceMeeting(
						map,
						contractId,
						legIndex,
						args.Generation?.SpaceMeetingChance ?? DeliveryGenerationConfig.DefaultSpaceMeetingChance)
					&& TryPickSpaceMeeting(map, contractId, legIndex, out var meeting))
				{
					legs.Add(meeting);
					continue;
				}

				var dropoff = DeliveryDropoffPicker.Pick(
					map,
					args.PickupPoiId,
					contractId,
					$"delivery-route-leg-{legIndex}",
					excludedPoiIds);
				legs.Add(new FacilityDeliveryLeg(
					dropoff.PoiId,
					dropoff.FacilityId,
					dropoff.OperatorName));
				excludedPoiIds.Add(dropoff.PoiId);
			}

			return new DeliveryRoute(legs);
		}

		if (args.Generation?.FacilityLegCount > 1)
			throw new ArgumentException(
				"Delivery dropoff overrides support only a single facility leg.",
				nameof(args));

		if (string.IsNullOrEmpty(args.DropoffPoiId)
			|| string.IsNullOrEmpty(args.DropoffFacilityId)
			|| string.IsNullOrEmpty(args.DropoffOperatorName))
			throw new ArgumentException(
				"Delivery dropoff override requires PoiId, FacilityId, and OperatorName.",
				nameof(args));

		return new DeliveryRoute(
			[new FacilityDeliveryLeg(
				args.DropoffPoiId,
				args.DropoffFacilityId,
				args.DropoffOperatorName)]);
	}

	private static bool ShouldGenerateSpaceMeeting(
		StarMap map,
		string contractId,
		int legIndex,
		double chance)
	{
		var random = new StableRandom(
			StableSeedMixer.From(map.Seed)
				.Add(contractId)
				.Add("delivery-space-meeting-roll")
				.Add(legIndex)
				.Value);
		return random.NextDouble() < chance;
	}

	private static bool TryPickSpaceMeeting(
		StarMap map,
		string contractId,
		int legIndex,
		out SpaceMeetingDeliveryLeg meeting)
	{
		var meetingId = $"{contractId}.meeting.{legIndex}";
		var pickerArgs = new AreaPickerArgs(
			MapLandmarkQueries.AllIds(map),
			DeterministicPickMix: (long)StableSeedMixer.From(map.Seed)
				.Add(meetingId)
				.Add("area")
				.Value);
		var seeds = CreateSpawnSeeds(map.Seed, contractId, meetingId, 1);
		if (!AreaPicker.TryPickWithFallback(map, pickerArgs, seeds, out var area))
		{
			meeting = null!;
			return false;
		}

		meeting = new SpaceMeetingDeliveryLeg(
			meetingId,
			"Delivery contact",
			area);
		return true;
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

		ArgumentNullException.ThrowIfNull(args.SearchAreaPicker);
		var wreckageId = WreckageIdFor(contractId);
		var spawnSeeds = CreateSpawnSeeds(map.Seed, contractId, wreckageId, 1);
		if (args.SearchAreaPicker.LandmarkCandidateIds.Count < 1
			|| !AreaPicker.TryPickWithFallback(map, args.SearchAreaPicker, spawnSeeds, out var searchArea))
			return false;

		var outcome = RollWreckageOutcome(map, contractId, args);
		var objective = new WreckageObjective(wreckageId, searchArea, outcome);
		contract = new Contract(
			contractId,
			objective,
			args.Danger,
			map.ControllingFaction,
			ResolvePayment(map.Seed, contractId, EContractKind.Wreckage, args.Danger),
			args.Narrative,
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

	internal static bool IsHuntObjectiveMet(string contractId, StarMap map, string actorId)
	{
		_ = actorId;
		return map.ContractRegistry.TryGetState(contractId, out var state)
			&& state is HuntContractState hunt
			&& hunt.IsObjectiveMet();
	}

	internal static bool IsDeliveryObjectiveMet(string contractId, StarMap map, string actorId)
	{
		_ = actorId;
		return map.ContractRegistry.TryGetState(contractId, out var state)
			&& state is DeliveryContractState delivery
			&& delivery.IsObjectiveMet();
	}

	internal static bool IsWreckageObjectiveMet(string contractId, StarMap map, string actorId)
	{
		_ = actorId;
		return map.ContractRegistry.TryGetState(contractId, out var state)
			&& state is WreckageContractState wreckage
			&& wreckage.IsObjectiveMet();
	}

}
