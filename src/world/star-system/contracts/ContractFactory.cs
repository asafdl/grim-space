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

		var contract = kind switch
		{
			EContractKind.Hunt => args switch
			{
				HuntCreateArgs huntArgs => CreateHunt(map, huntArgs),
				_ => throw Mismatch(kind, args),
			},
			EContractKind.Delivery => args switch
			{
				DeliveryCreateArgs => throw new NotSupportedException(
					$"Contract kind '{kind}' is not implemented yet."),
				_ => throw Mismatch(kind, args),
			},
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};

		map.ContractRegistry.RegisterOffered(contract);
		return contract;
	}

	private static ArgumentException Mismatch(EContractKind kind, ContractCreateArgs args) =>
		new($"Contract kind '{kind}' requires matching create args, but received '{args.GetType().Name}'.", nameof(args));

	private static Contract CreateHunt(StarMap map, HuntCreateArgs args)
	{
		ArgumentException.ThrowIfNullOrEmpty(args.IssuerPoiId);

		var searchArea = PickSearchArea(map, args.SearchAreaPicker)
			?? throw new InvalidOperationException(
				$"Could not pick a hunt search area for map seed {map.Seed}.");

		var contractId = TypedIdGenerator.NextId("contract");
		var groupId = TypedIdGenerator.NextId("spawn-group");
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
						picker.MinLandmarkSeparation);
				}
				catch (InvalidOperationException)
				{
				}
			}
		}

		return null;
	}
}
