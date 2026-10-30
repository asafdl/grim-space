using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Economy;

public static class StrategicEconomyConfig
{
	public const int StartingCredits = 500;
	public const int StartingScrapAlloy = 100;
	public const int StartingFuel = 100;
	public const int FuelPurchaseAmount = 100;
	public const int FuelPurchaseCreditCost = 50;
	public const double DistancePerFuelUnit = 25;
	public const int StarvationDamagePerTick = 2;
	public const int StrategicTicksPerSecond = 5;
	public const int SalaryCadenceMinutes = 5;

	public static ResourceBundle StartingResources { get; } =
		ResourceBundle.Create(
			(ResourceId.Credits, StartingCredits),
			(ResourceId.ScrapAlloy, StartingScrapAlloy),
			(ResourceId.Fuel, StartingFuel));

	public const int SalaryCreditsPerLevel = 5;
	public const int SalaryCadenceTicks =
		StrategicTicksPerSecond * 60 * SalaryCadenceMinutes;
	public const int DefaultBaseSalaryCredits = 60;
	public const int BaseSalaryCreditsMin = 50;
	public const int BaseSalaryCreditsMax = 75;

	public const int HazardPayCreditsPerPilot = 10;
	public const int FleeFuelPerPilot = 10;
	public const double FleeFuelLevelMultiplier = 1.5;

	public const int RefuseChanceBasePercent = 35;
	public const int RefuseChanceRisePercent = 25;

	public const int DepartChanceAfterFirstPercent = 2;
	public const int DepartChanceRisePercent = 15;

	public static int SalaryFor(int baseSalaryCredits, int claimedLevel)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(baseSalaryCredits);
		ArgumentOutOfRangeException.ThrowIfLessThan(claimedLevel, 1);
		return baseSalaryCredits + (claimedLevel - 1) * SalaryCreditsPerLevel;
	}

	public static int FleeFuelForLevel(int level)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
		return checked((int)System.Math.Ceiling(
			FleeFuelPerPilot * System.Math.Pow(FleeFuelLevelMultiplier, level - 1)));
	}

	public static int RollBaseSalaryCredits(Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		return rng.Next(BaseSalaryCreditsMin, BaseSalaryCreditsMax + 1);
	}

	public static int RefuseChancePercent(int missedHazardPeriods)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(missedHazardPeriods);
		if (missedHazardPeriods <= 0)
			return 0;

		return System.Math.Min(
			100,
			RefuseChanceBasePercent + (missedHazardPeriods - 1) * RefuseChanceRisePercent);
	}

	public static int DepartChancePercent(int missedSalaryPeriods)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(missedSalaryPeriods);
		if (missedSalaryPeriods <= 1)
			return 0;

		return System.Math.Min(
			100,
			DepartChanceAfterFirstPercent + (missedSalaryPeriods - 2) * DepartChanceRisePercent);
	}
}
