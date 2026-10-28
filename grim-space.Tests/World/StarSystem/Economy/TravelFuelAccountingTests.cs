using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.Tests.World.StarSystem.Economy;

public sealed class TravelFuelAccountingTests
{
	[Fact]
	public void Charge_BurnsWholeUnitsAndKeepsFractionalRemainder()
	{
		var distancePerFuel = StrategicEconomyConfig.DistancePerFuelUnit;
		var result = TravelFuelAccounting.Charge(
			TravelFuelAccounting.Cleared,
			journeyId: 1,
			distanceAlongJourney: distancePerFuel * 2.5,
			availableFuel: 10);

		Assert.Equal(2, result.FuelToBurn);
		Assert.Equal(distancePerFuel * 0.5, result.Ledger.UnchargedDistance);
		Assert.Equal(distancePerFuel * 2.5, result.Ledger.ChargedDistanceAlongCurrentJourney);
		Assert.Equal(1, result.Ledger.JourneyId);
		Assert.False(result.Starving);
	}

	[Fact]
	public void Charge_RepathKeepsUnchargedRemainderWithoutDoubleCharging()
	{
		var distancePerFuel = StrategicEconomyConfig.DistancePerFuelUnit;
		var afterFirst = TravelFuelAccounting.Charge(
			TravelFuelAccounting.Cleared,
			journeyId: 1,
			distanceAlongJourney: distancePerFuel * 2.5,
			availableFuel: 10);

		var afterRepath = TravelFuelAccounting.Charge(
			afterFirst.Ledger,
			journeyId: 2,
			distanceAlongJourney: distancePerFuel * 0.4,
			availableFuel: 10 - afterFirst.FuelToBurn);

		Assert.Equal(0, afterRepath.FuelToBurn);
		Assert.Equal(distancePerFuel * 0.9, afterRepath.Ledger.UnchargedDistance);
		Assert.Equal(distancePerFuel * 0.4, afterRepath.Ledger.ChargedDistanceAlongCurrentJourney);
		Assert.Equal(2, afterRepath.Ledger.JourneyId);
	}

	[Fact]
	public void Charge_CapsBurnToAvailableFuelAndMarksStarving()
	{
		var distancePerFuel = StrategicEconomyConfig.DistancePerFuelUnit;
		var result = TravelFuelAccounting.Charge(
			TravelFuelAccounting.Cleared,
			journeyId: 1,
			distanceAlongJourney: distancePerFuel * 10,
			availableFuel: 1);

		Assert.Equal(1, result.FuelToBurn);
		Assert.True(result.Starving);
		Assert.Equal(distancePerFuel * 9, result.Ledger.UnchargedDistance);
	}

	[Fact]
	public void FuelUnitsForDistance_CeilsPartialUnits()
	{
		var distancePerFuel = StrategicEconomyConfig.DistancePerFuelUnit;
		Assert.Equal(0, TravelFuelAccounting.FuelUnitsForDistance(0));
		Assert.Equal(1, TravelFuelAccounting.FuelUnitsForDistance(distancePerFuel * 0.1));
		Assert.Equal(3, TravelFuelAccounting.FuelUnitsForDistance(distancePerFuel * 2.1));
	}
}
