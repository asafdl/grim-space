using GrimSpace.Battle.Objectives;
using GrimSpace.Math;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run;

/// <summary>Salvage rolled from destroyed ships (battle) or wreckage contracts. Power-based bonuses use <see cref="ShipPowerCatalog"/>.</summary>
public static class LootCatalog
{
	private const int PatrolScrapMin = 50;
	private const int PatrolScrapMaxExclusive = 121;
	private const int CarrierScrapMin = 140;
	private const int CarrierScrapMaxExclusive = 281;
	private const int ScrapBonusPercentPerGearTier = 15;
	private const int HighPowerIndustrialCoreThreshold = 10;
	private const float IndustrialCoreDropChance = 0.08f;

	//TODO: need to find a better system, battle outcome is just the outcome of battle, has no concept of friend foe
	public static LootResult For(BattleOutcome outcome)
	{
		var rolls = new List<LootRoll>();
		foreach (var handoff in outcome.StateHandoffs)
		{
			if (handoff.HullPoints > 0)
				continue;

			var awarded = SalvageFromShip(handoff.Chassis, handoff.GearTier);
			if (!awarded.IsEmpty)
				rolls.Add(new LootRoll(handoff.Id, handoff.Chassis, awarded));
		}

		return new LootResult(rolls, SumRolls(rolls));
	}

	public static ResourceBundle SalvageFromShip(ShipPowerLevel wreck, StableRandom rng) =>
		SalvageFromShip(wreck.Chassis, wreck.Tier, wreck.Power, rng);

	public static ResourceBundle SalvageFromShip(EType chassis, EShipGearTier gearTier) =>
		SalvageFromShip(chassis, gearTier, Random.Shared);

	public static ResourceBundle SalvageFromShip(EType chassis, EShipGearTier gearTier, StableRandom rng) =>
		SalvageFromShip(chassis, gearTier, ShipPowerCatalog.PowerFor(chassis, gearTier), rng);

	private static ResourceBundle SalvageFromShip(
		EType chassis,
		EShipGearTier gearTier,
		int power,
		StableRandom rng)
	{
		if (!TryRollScrapAmount(chassis, gearTier, value => NextInt(rng, value.min, value.maxExclusive), out var scrapAmount))
			return ResourceBundle.Empty;

		return AwardSalvage(scrapAmount, power, rng.NextDouble);
	}

	private static ResourceBundle SalvageFromShip(EType chassis, EShipGearTier gearTier, Random random)
	{
		if (!TryRollScrapAmount(chassis, gearTier, range => random.Next(range.min, range.maxExclusive), out var scrapAmount))
			return ResourceBundle.Empty;

		var power = ShipPowerCatalog.PowerFor(chassis, gearTier);
		return AwardSalvage(scrapAmount, power, random.NextDouble);
	}

	private static bool TryRollScrapAmount(
		EType chassis,
		EShipGearTier gearTier,
		Func<(int min, int maxExclusive), int> rollInRange,
		out int scrapAmount)
	{
		switch (chassis)
		{
			case EType.Patrol:
				scrapAmount = ScaleScrap(
					rollInRange((PatrolScrapMin, PatrolScrapMaxExclusive)),
					gearTier);
				return true;
			case EType.Carrier:
				scrapAmount = ScaleScrap(
					rollInRange((CarrierScrapMin, CarrierScrapMaxExclusive)),
					gearTier);
				return true;
			default:
				scrapAmount = 0;
				return false;
		}
	}

	private static int NextInt(StableRandom rng, int min, int maxExclusive) =>
		min + (int)(rng.NextDouble() * (maxExclusive - min));

	private static ResourceBundle AwardSalvage(int scrapAmount, int power, Func<double> roll)
	{
		if (power > HighPowerIndustrialCoreThreshold && roll() < IndustrialCoreDropChance)
		{
			return ResourceBundle.Create(
				(ResourceId.ScrapAlloy, scrapAmount),
				(ResourceId.IndustrialCore, 1));
		}

		return ResourceBundle.Of(ResourceId.ScrapAlloy, scrapAmount);
	}

	private static int ScaleScrap(int baseScrap, EShipGearTier gearTier)
	{
		var bonusPercent = (int)gearTier * ScrapBonusPercentPerGearTier;
		return (int)System.Math.Round(baseScrap * (100 + bonusPercent) / 100.0);
	}

	private static ResourceBundle SumRolls(IReadOnlyList<LootRoll> rolls)
	{
		var totals = new Dictionary<ResourceId, int>();
		foreach (var roll in rolls)
		{
			foreach (var (id, amount) in roll.Awarded)
				totals[id] = totals.GetValueOrDefault(id) + amount;
		}

		return ResourceBundle.Create(totals);
	}
}

public sealed record LootResult(IReadOnlyList<LootRoll> Rolls, ResourceBundle Total);

public sealed record LootRoll(string TacticalUnitId, EType Kind, ResourceBundle Awarded);
