using GrimSpace.Units.Enums;

namespace GrimSpace.Units;

internal static class ShipLoadoutTierRoller
{
	public const int CostInstallWeapon = 5;
	public const int CostWeaponDamage = 4;
	public const int CostHullMax = 3;
	public const int CostWeaponRange = 2;
	public const int CostShieldMax = 1;

	public static int PowerBudget(EShipGearTier tier) =>
		tier switch
		{
			EShipGearTier.T0 => 0,
			EShipGearTier.T1 => 4,
			EShipGearTier.T2 => 8,
			EShipGearTier.T3 => 12,
			_ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
		};

	public static ShipLoadout Roll(EType chassis, EShipGearTier tier, int? rollSeed, ShipLoadout baseline)
	{
		var budget = PowerBudget(tier);
		if (budget <= 0)
			return baseline.DeepCopy();

		var spec = ShipCatalog.SpecFor(chassis);
		var rng = rollSeed is int seed ? new Random(seed) : null;
		var takenOnce = new HashSet<EChassisBuffOffer>();
		var rangeSteps = 0;
		var picked = PowerBudgetRoller.Spend(
			budget,
			_ => ChassisBuffCatalog.All
				.Where(offer => ChassisBuffCatalog.IsAvailable(
					offer,
					spec,
					baseline,
					takenOnce,
					rangeSteps))
				.ToArray(),
			ChassisBuffCatalog.NetCostOf,
			rng is null ? null : count => rng.Next(count),
			Comparer<EChassisBuffOffer>.Default,
			offer =>
			{
				if (ChassisBuffCatalog.IsOnce(offer))
					takenOnce.Add(offer);
				if (ChassisBuffCatalog.GrantsRange(offer))
					rangeSteps++;
			});

		return ChassisBuffCatalog.Apply(spec, baseline, picked);
	}
}
