using BattleUnitType = GrimSpace.Units.Enums.EType;
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

		var contract = ContractFactory.Create(map, EContractKind.Hunt, CreateBeatAHuntArgs(map));
		return contract.Id;
	}

	internal static HuntCreateArgs CreateBeatAHuntArgs(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		return new HuntCreateArgs(
			plan.AdministrativePoiId,
			new AreaPickerArgs(plan.OperationalPoiIds),
			new HuntEncounterArgs(
				FleetType.PirateFleet,
				EFaction.Pirates,
				EDangerLevel.VeryLow,
				[
					BattleUnitType.Patrol,
					BattleUnitType.Patrol,
					BattleUnitType.Patrol,
				]),
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, BeatAHuntRewardCredits)),
			ContractNarrative.ForHunt("Pirate Hunt"),
			IsStoryObjective: true);
	}

	internal static DeliveryCreateArgs CreateBeatBDeliveryArgs(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		var exitPoiId = plan.ExitPoiId;
		var travelFacilityId = Facility.ScopedId(exitPoiId, Wormhole.TravelFacilitySlug);
		var exitPoi = map.PointsOfInterest.First(poi => poi.Id == exitPoiId);
		var travelFacility = exitPoi.Facilities.First(facility => facility.Id == travelFacilityId);
		var travelOperatorName = travelFacility.Operators
			.First(operatorEntry => operatorEntry.Role == EFacilityOperatorRole.Dialog)
			.Name;

		return new DeliveryCreateArgs(
			plan.StoragePoiId,
			new ContractTerms(ResourceBundle.Of(ResourceId.Credits, BeatBDeliveryRewardCredits)),
			ContractNarrative.ForDelivery(
				"Supply Run",
				$"We have this package for a dude {travelOperatorName} at Wormhole Travel. He's kinda weird, I don't want to deal with him so I'll pay you to do it.",
				"Fuuuuucking Finally!\nI've been waiting in this hellhole for 352.1123221119 days already. The Optimality idiots say that all travel is stopped until the demo is completed.\nFucking Calculators, efficient my ass!\nWell anyways... Thanks for bringing me my lubricant, I need it for... stuff..."),
			IsStoryObjective: true,
			DropoffPoiId: exitPoiId,
			DropoffFacilityId: travelFacilityId,
			DropoffOperatorName: travelOperatorName);
	}

	public static string OfferBeatB(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		var contract = ContractFactory.Create(map, EContractKind.Delivery, CreateBeatBDeliveryArgs(map));
		return contract.Id;
	}
}
