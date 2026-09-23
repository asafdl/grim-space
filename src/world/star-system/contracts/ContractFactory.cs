using GrimSpace.Core.Ids;
using GrimSpace.Math;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts.Objectives;

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
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};
	}

	private static ArgumentException Mismatch(EContractKind kind, ContractCreateArgs args) =>
		new($"Contract kind '{kind}' requires matching create args, but received '{args.GetType().Name}'.", nameof(args));

	private static Contract BuildHunt(StarMap map, string contractId, HuntCreateArgs args)
	{
		ArgumentException.ThrowIfNullOrEmpty(args.IssuerPoiId);

		var searchArea = PickSearchArea(map, args.SearchAreaPicker)
			?? throw new InvalidOperationException(
				$"Could not pick a hunt search area for map seed {map.Seed}.");

		var groupId = SpawnGroupIdFor(contractId);
		var spawnSeed = unchecked((int)StableSeedMixer.From(map.Seed).Add(contractId).Add(groupId).Value);
		var encounter = args.Encounter;
		var objective = new HuntObjective(
		[
			new SpawnEncounterGroup(
				groupId,
				searchArea,
				1,
				new FleetSpawnSpec(
					encounter.FleetType,
					encounter.Faction,
					encounter.Danger,
					spawnSeed,
					encounter.MemberTypes)),
		]);

		return new Contract(
			contractId,
			objective,
			map.ControllingFaction,
			args.IssuerPoiId,
			args.Terms,
			args.Narrative,
			args.IsStoryObjective);
	}

	private static Contract BuildDelivery(StarMap map, string contractId, DeliveryCreateArgs args)
	{
		ArgumentException.ThrowIfNullOrEmpty(args.IssuerPoiId);

		var (turnInPoiId, turnInFacilityId, turnInOperatorName) = ResolveDropoff(map, args, contractId);
		var objective = new DeliveryObjective(turnInPoiId, turnInFacilityId, turnInOperatorName);

		return new Contract(
			contractId,
			objective,
			map.ControllingFaction,
			args.IssuerPoiId,
			args.Terms,
			args.Narrative,
			args.IsStoryObjective);
	}

	internal static string SpawnGroupIdFor(string contractId) => $"{contractId}.spawns";

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

	private static AreaPick? PickSearchArea(StarMap map, AreaPickerArgs picker)
	{
		foreach (var group in picker.LandmarkGroups)
		{
			foreach (var distance in picker.Distances)
			{
				try
				{
					return AreaPicker.Pick(
						map,
						[group],
						[distance],
						picker.MinLandmarkSeparation,
						deterministicPickMix: picker.DeterministicPickMix);
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		return null;
	}
}
