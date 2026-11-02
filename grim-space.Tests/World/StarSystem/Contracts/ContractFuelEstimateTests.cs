using GrimSpace.Math.Grid;
using GrimSpace.Math.Routes;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tests.World.StarSystem;
using FleetType = GrimSpace.World.StarSystem.Units.EType;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractFuelEstimateTests(StarMapFixture maps)
{
	[Fact]
	public void Estimate_Hunt_UsesNearestSpawn()
	{
		var map = maps.Fresh(42);
		var origin = map.PointsOfInterest[0];
		var near = new Coord(origin.PlacedCenter.X + 25, 0, origin.PlacedCenter.Z);
		var far = new Coord(origin.PlacedCenter.X + 100, 0, origin.PlacedCenter.Z);
		var contract = ContractWith(new HuntObjective(
		[
			new SpawnEncounterGroup(
				"group",
				new AreaPick(Intel(), [far, near]),
				1,
				new FleetSpawnSpec(FleetType.PirateFleet, EFaction.Pirates, 1, [])),
		]));

		Assert.True(ContractDisplay.TryEstimateFuelDistance(contract, map, origin.Id, out var fuel));
		Assert.Equal(1, fuel);
	}

	[Fact]
	public void Estimate_Wreckage_RoundsDistanceUpToFuel()
	{
		var map = maps.Fresh(42);
		var origin = map.PointsOfInterest[0];
		var site = new Coord(origin.PlacedCenter.X + 26, 0, origin.PlacedCenter.Z);
		var contract = ContractWith(new WreckageObjective(
			"wreck",
			new AreaPick(Intel(), [site]),
			new WreckageOutcome.Salvage(ResourceBundle.Empty)));

		Assert.True(ContractDisplay.TryEstimateFuelDistance(contract, map, origin.Id, out var fuel));
		Assert.Equal(TravelFuelAccounting.FuelUnitsForDistance(26), fuel);
		Assert.Equal(2, fuel);
	}

	[Fact]
	public void Estimate_Delivery_SumsPickupAndLegsFromOffer()
	{
		var map = maps.Fresh(42);
		var origin = map.PointsOfInterest[0];
		var pickup = map.PointsOfInterest[1];
		var contract = ContractWith(new DeliveryObjective(
			pickup.Id,
			origin.Id,
			"facility",
			"operator"));

		var distance = RouteGeometry.Distance(origin.PlacedCenter, pickup.PlacedCenter) * 2;
		Assert.True(ContractDisplay.TryEstimateFuelDistance(contract, map, origin.Id, out var fuel));
		Assert.Equal(TravelFuelAccounting.FuelUnitsForDistance(distance), fuel);
		Assert.NotEqual(0, fuel);
	}

	[Fact]
	public void Estimate_LeadFacility_UsesDestination()
	{
		var map = maps.Fresh(42);
		var origin = map.PointsOfInterest[0];
		var destination = map.PointsOfInterest[1];
		var contract = ContractWith(
			new BlackMarketSaleObjective(
				new BlackMarketFacilityDestination(destination.Id, "facility"),
				EMerchantSubtype.Ships,
				10),
			EContractCategory.Lead);

		var distance = RouteGeometry.Distance(origin.PlacedCenter, destination.PlacedCenter);
		Assert.True(ContractDisplay.TryEstimateFuelDistance(contract, map, origin.Id, out var fuel));
		Assert.Equal(TravelFuelAccounting.FuelUnitsForDistance(distance), fuel);
	}

	[Fact]
	public void Estimate_UnknownOrigin_ReturnsFalse()
	{
		var map = maps.Fresh(42);
		var contract = ContractWith(new DeliveryObjective(
			map.PointsOfInterest[0].Id,
			map.PointsOfInterest[1].Id,
			"facility",
			"operator"));

		Assert.False(ContractDisplay.TryEstimateFuelDistance(contract, map, "missing", out _));
	}

	private static Contract ContractWith(
		IContractObjective objective,
		EContractCategory category = EContractCategory.Standard) =>
		new(
			"fuel-estimate",
			objective,
			EDangerLevel.Low,
			EFaction.Pirates,
			new ContractTerms(ResourceBundle.Empty),
			new ContractNarrative("Test", "Briefing."),
			Category: category);

	private static AreaIntel Intel() => new("Somewhere near {A}.", "a", "b", "c");
}
