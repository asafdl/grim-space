using GrimSpace.Math;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts.Encounter;

public static class EncounterBudgetRoller
{
	public const int MaxEnemies = 6;

	private const double CheapBiasVeryLow = 2.0;
	private const double CheapBiasLow = 2.0;
	private const double CheapBiasModerate = 1.5;
	private const double CheapBiasHigh = 1.0;
	private const double CheapBiasVeryHigh = 0.5;

	private static readonly ShipPowerLevel[] Catalog = ShipPowerCatalog.All
		.OrderBy(level => level.Chassis)
		.ThenBy(level => level.Tier)
		.ToArray();

	public static IReadOnlyList<(EType Chassis, EShipGearTier GearTier)> Roll(
		int mapSeed,
		string contractId,
		string scope,
		EDangerLevel danger) =>
		Roll(mapSeed, contractId, scope, danger, CheapBiasExponent(danger));

	internal static IReadOnlyList<(EType Chassis, EShipGearTier GearTier)> Roll(
		int mapSeed,
		string contractId,
		string scope,
		EDangerLevel danger,
		double cheapBiasExponent)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentException.ThrowIfNullOrEmpty(scope);
		if (cheapBiasExponent < 0)
			throw new ArgumentOutOfRangeException(nameof(cheapBiasExponent));

		var budget = EncounterPowerCatalog.EncounterPowerBudget(danger);
		var rng = new StableRandom(
			StableSeedMixer.From(mapSeed).Add(contractId).Add(scope).Value);
		var remaining = budget;
		var pickedCount = 0;
		var picked = PowerBudgetRoller.Spend(
			budget,
			_ => pickedCount >= MaxEnemies ? [] : Catalog,
			level => level.Power,
			_ => WeightedPickIndex(Affordable(remaining), cheapBiasExponent, rng.NextDouble()),
			canonicalOrder: null,
			onPicked: level =>
			{
				pickedCount++;
				remaining -= level.Power;
			});
		var ships = picked
			.Select(level => (level.Chassis, level.Tier))
			.ToList();

		if (ships.Count == 0)
			ships.Add((EType.RepurposedMiner, EShipGearTier.T0));

		return ships;
	}

	private static double CheapBiasExponent(EDangerLevel danger) =>
		danger switch
		{
			EDangerLevel.VeryLow => CheapBiasVeryLow,
			EDangerLevel.Low => CheapBiasLow,
			EDangerLevel.Moderate => CheapBiasModerate,
			EDangerLevel.High => CheapBiasHigh,
			EDangerLevel.VeryHigh => CheapBiasVeryHigh,
			_ => throw new ArgumentOutOfRangeException(nameof(danger), danger, null),
		};

	private static List<ShipPowerLevel> Affordable(int remaining)
	{
		var affordable = new List<ShipPowerLevel>();
		foreach (var level in Catalog)
		{
			if (level.Power <= remaining)
				affordable.Add(level);
		}

		return affordable;
	}

	private static int WeightedPickIndex(
		IReadOnlyList<ShipPowerLevel> affordable,
		double exponent,
		double roll)
	{
		var total = 0.0;
		foreach (var level in affordable)
			total += CheapWeight(level.Power, exponent);

		var mark = roll * total;
		var cumulative = 0.0;
		for (var index = 0; index < affordable.Count; index++)
		{
			cumulative += CheapWeight(affordable[index].Power, exponent);
			if (mark < cumulative)
				return index;
		}

		return affordable.Count - 1;
	}

	private static double CheapWeight(int power, double exponent) =>
		1.0 / System.Math.Pow(power, exponent);
}
