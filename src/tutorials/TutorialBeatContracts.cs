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
			return map.ContractRegistry.All
				.First(contract => contract.IsStoryObjective && contract.Objective is HuntObjective)
				.Id;

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
			plan.AdministrativePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, BeatAHuntRewardCredits)),
			ContractNarrative.ForHunt("Pirate Hunt"),
			ContractFactory.IsHuntObjectiveMet,
			IsStoryObjective: true);
		if (!map.ContractRegistry.TryAdd(contract))
			throw new InvalidOperationException($"Failed to add tutorial hunt contract '{contractId}'.");
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
			new DeliveryObjective(dropoff.PoiId, dropoff.FacilityId, dropoff.OperatorName),
			EDangerLevel.VeryLow,
			map.ControllingFaction,
			plan.StoragePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, BeatBDeliveryRewardCredits)),
			ContractNarrative.ForDelivery(
				"Supply Run",
				$"We have this package for a dude {dropoff.OperatorName} at Wormhole Travel. He's kinda weird, I don't want to deal with him so I'll pay you to do it.",
				"Fuuuuucking Finally!\nI've been waiting in this hellhole for 352.1123221119 days already. The Optimality idiots say that all travel is stopped until the demo is completed.\nFucking Calculators, efficient my ass!\nWell anyways... Thanks for bringing me my lubricant, I need it for... stuff..."),
			ContractFactory.IsDeliveryObjectiveMet,
			IsStoryObjective: true);
		if (!map.ContractRegistry.TryAdd(contract))
			throw new InvalidOperationException($"Failed to add tutorial delivery contract '{contractId}'.");
		return contract.Id;
	}

	internal static (string IssuerPoiId, string DropoffPoiId, string DropoffFacilityId, string DropoffOperatorName)
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

	private static string BeatAContractId(int mapSeed) => $"tutorial-beat-a-hunt-{mapSeed}";

	private static string BeatBContractId(int mapSeed) => $"tutorial-beat-b-delivery-{mapSeed}";
}
