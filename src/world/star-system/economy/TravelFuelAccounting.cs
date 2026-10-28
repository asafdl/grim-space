namespace GrimSpace.World.StarSystem.Economy;

public readonly record struct TravelFuelLedgerState(
	long JourneyId,
	double ChargedDistanceAlongCurrentJourney,
	double UnchargedDistance);

public readonly record struct TravelFuelChargeResult(
	TravelFuelLedgerState Ledger,
	int FuelToBurn,
	bool Starving);

public static class TravelFuelAccounting
{
	public static int FuelUnitsForDistance(double distance)
	{
		if (distance <= 0)
			return 0;

		return (int)System.Math.Ceiling(distance / StrategicEconomyConfig.DistancePerFuelUnit);
	}

	public static TravelFuelChargeResult Charge(
		TravelFuelLedgerState ledger,
		long journeyId,
		double distanceAlongJourney,
		int availableFuel)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(availableFuel);
		var along = System.Math.Clamp(distanceAlongJourney, 0, double.MaxValue);
		var chargedAlong = journeyId == ledger.JourneyId
			? System.Math.Max(0, ledger.ChargedDistanceAlongCurrentJourney)
			: 0;
		var newlyTraveled = System.Math.Max(0, along - chargedAlong);
		var uncharged = System.Math.Max(0, ledger.UnchargedDistance) + newlyTraveled;
		var unitsFromDistance = (int)System.Math.Floor(
			uncharged / StrategicEconomyConfig.DistancePerFuelUnit);
		var fuelToBurn = System.Math.Min(availableFuel, unitsFromDistance);
		uncharged -= fuelToBurn * StrategicEconomyConfig.DistancePerFuelUnit;
		var remainingFuel = availableFuel - fuelToBurn;
		return new TravelFuelChargeResult(
			new TravelFuelLedgerState(journeyId, along, uncharged),
			fuelToBurn,
			remainingFuel == 0);
	}

	public static TravelFuelLedgerState Cleared { get; } = new(0, 0, 0);
}
