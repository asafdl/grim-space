using GrimSpace.Math;
using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

/// <summary>Chassis and gear tier with encounter power cost (used for budgets, intel, and salvage tier).</summary>
public readonly record struct ShipPowerLevel(EType Chassis, EShipGearTier Tier, int Power);

/// <summary>Authoritative power costs per chassis/tier. Encounter budgets spend this power.</summary>
public static class ShipPowerCatalog
{
	private static readonly ShipPowerLevel[] Levels =
	[
		new(EType.Patrol, EShipGearTier.T0, 1),
		new(EType.Patrol, EShipGearTier.T1, 2),
		new(EType.Patrol, EShipGearTier.T2, 4),
		new(EType.Patrol, EShipGearTier.T3, 8),
		new(EType.Carrier, EShipGearTier.T0, 11),
	];

	public static IEnumerable<ShipPowerLevel> All => Levels;

	public static IEnumerable<ShipPowerLevel> Affordable(int remainingPower) =>
		Levels.Where(level => level.Power <= remainingPower);

	public static int PowerFor(EType chassis, EShipGearTier tier)
	{
		foreach (var level in Levels)
		{
			if (level.Chassis == chassis && level.Tier == tier)
				return level.Power;
		}

		return 0;
	}

	public static ShipPowerLevel PickRandomWithinBudget(StableRandom rng, int powerBudget)
	{
		var affordable = Affordable(powerBudget).ToArray();
		if (affordable.Length == 0)
			return Levels[0];

		return affordable[(int)(rng.NextDouble() * affordable.Length)];
	}
}
