using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.World.StarSystem.Resources;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.Tutorials;

public static class TutorialBeatContracts
{
	internal const int BeatAHuntRewardCredits = 100;
	internal const int BeatBDeliveryRewardCredits = 75;

	public static string? OfferBeatA(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		if (map.ContractRegistry.All.Any(
				contract => contract.IsStoryObjective && contract.Objective is HuntObjective))
		{
			var existingId = map.ContractRegistry.All
				.First(contract => contract.IsStoryObjective && contract.Objective is HuntObjective)
				.Id;
			EnsureStoryContact(
				map,
				map.Blueprint.SupplyPlan.AdministrativePoiId,
				AdministrativeCore.ManagementFacilitySlug,
				existingId);
			return existingId;
		}

		var contractId = BeatAContractId(map.Seed);
		var plan = map.Blueprint.SupplyPlan;
		var areaArgs = new AreaPickerArgs(plan.OperationalPoiIds, DeterministicPickMix: map.Seed);
		var objective = ContractFactory.CreateHuntObjective(
			map,
			contractId,
			areaArgs,
			EDangerLevel.VeryLow,
			FleetType.PirateFleet,
			EFaction.Pirates);
		var contract = new Contract(
			contractId,
			objective,
			EDangerLevel.VeryLow,
			map.ControllingFaction,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, BeatAHuntRewardCredits)),
			ContractNarrative.ForHunt("Pirate Hunt"),
			IsStoryObjective: true);
		if (!map.ContractRegistry.TryAdd(contract))
			throw new InvalidOperationException($"Failed to add tutorial hunt contract '{contractId}'.");
		EnsureStoryContact(
			map,
			plan.AdministrativePoiId,
			AdministrativeCore.ManagementFacilitySlug,
			contract.Id);
		return contract.Id;
	}

	public static string OfferBeatB(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		var contractId = BeatBContractId(map.Seed);
		var plan = map.Blueprint.SupplyPlan;
		var dropoff = ResolveBeatBDropoff(map);
		var contract = new Contract(
			contractId,
			new DeliveryObjective(
				plan.StoragePoiId,
				dropoff.PoiId,
				dropoff.FacilityId,
				dropoff.OperatorName),
			EDangerLevel.VeryLow,
			map.ControllingFaction,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, BeatBDeliveryRewardCredits)),
			ContractNarrative.ForDelivery(
				"Supply Run",
				$"We have this package for a dude {dropoff.OperatorName} at Wormhole Travel. He's kinda weird, I don't want to deal with him so I'll pay you to do it.",
				"De-frag-ing Finally!\nI've been waiting in this rustbox for 352.1123221119 days already. The Optimality idiots say that all travel is stopped until the demo is completed.\nSeg-faulting Calculators, efficient my ass!\nWell anyways... Thanks for bringing me my lubricant, I need it for... stuff..."),
			IsStoryObjective: true);
		if (!map.ContractRegistry.TryAdd(contract))
			throw new InvalidOperationException($"Failed to add tutorial delivery contract '{contractId}'.");
		EnsureStoryContact(
			map,
			plan.StoragePoiId,
			StorageFacility.WarehouseFacilitySlug,
			contract.Id);
		return contract.Id;
	}

	public static void ReconcileStoryContacts(
		StarMap map,
		string? beatAContractId,
		string? beatBContractId)
	{
		ArgumentNullException.ThrowIfNull(map);

		var pendingStoryIds = new[] { beatAContractId, beatBContractId }
			.Where(contractId => contractId is not null
				&& map.ContractRegistry.IsPending(contractId)
				&& map.ContractRegistry.TryGet(contractId, out var contract)
				&& contract.IsStoryObjective)
			.Cast<string>()
			.ToHashSet(StringComparer.Ordinal);
		foreach (var poi in map.PointsOfInterest)
			poi.OperatorTemporaryRoles.PruneSources(
				EFacilityOperatorRole.StoryContact,
				pendingStoryIds);

		if (beatAContractId is { } beatAId && pendingStoryIds.Contains(beatAId))
		{
			EnsureStoryContact(
				map,
				map.Blueprint.SupplyPlan.AdministrativePoiId,
				AdministrativeCore.ManagementFacilitySlug,
				beatAId);
		}

		if (beatBContractId is { } beatBId && pendingStoryIds.Contains(beatBId))
		{
			EnsureStoryContact(
				map,
				map.Blueprint.SupplyPlan.StoragePoiId,
				StorageFacility.WarehouseFacilitySlug,
				beatBId);
		}
	}

	internal static (string PickupPoiId, string DropoffPoiId, string DropoffFacilityId, string DropoffOperatorName)
		BeatBDropoff(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		var dropoff = ResolveBeatBDropoff(map);
		return (plan.StoragePoiId, dropoff.PoiId, dropoff.FacilityId, dropoff.OperatorName);
	}

	private static (string PoiId, string FacilityId, string OperatorName) ResolveBeatBDropoff(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		var exitPoiId = plan.ExitPoiId;
		var travelFacilityId = Facility.ScopedId(exitPoiId, Wormhole.TravelFacilitySlug);
		var exitPoi = map.PointsOfInterest.First(poi => poi.Id == exitPoiId);
		var travelFacility = exitPoi.Facilities.First(facility => facility.Id == travelFacilityId);
		var travelOperatorName = travelFacility.Operators
			.First(operatorEntry => operatorEntry.Role == EFacilityOperatorRole.Dialog)
			.Name;
		return (exitPoiId, travelFacilityId, travelOperatorName);
	}

	private static void EnsureStoryContact(
		StarMap map,
		string poiId,
		string facilitySlug,
		string contractId)
	{
		var poi = map.GetPointOfInterest(poiId);
		var facilityId = Facility.ScopedId(poiId, facilitySlug);
		var facility = poi.GetFacility(facilityId);
		var facilityOperator = facility.Operators.Single();
		var roles = poi.OperatorTemporaryRoles;
		if (roles.TryGetAssignment(facilityId, facilityOperator.Name, out var assignment))
		{
			if (assignment.Role == EFacilityOperatorRole.StoryContact)
			{
				roles.Grant(
					facilityId,
					facilityOperator.Name,
					EFacilityOperatorRole.StoryContact,
					contractId);
				return;
			}

			if (assignment.Role != EFacilityOperatorRole.Contracts)
			{
				throw new InvalidOperationException(
					$"Story contact '{facilityOperator.Name}' already has temporary role " +
					$"'{assignment.Role}'.");
			}

			foreach (var sourceId in assignment.SourceIds)
				roles.RevokeSource(facilityId, facilityOperator.Name, sourceId);
		}

		roles.Grant(
			facilityId,
			facilityOperator.Name,
			EFacilityOperatorRole.StoryContact,
			contractId);
	}

	private static string BeatAContractId(int mapSeed) => $"tutorial-beat-a-hunt-{mapSeed}";

	private static string BeatBContractId(int mapSeed) => $"tutorial-beat-b-delivery-{mapSeed}";
}
