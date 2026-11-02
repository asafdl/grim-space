using GrimSpace.Math;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts.Encounter;

public static class EncounterBudgetRoller
{
	public static IReadOnlyList<(EType Chassis, EShipGearTier GearTier)> Roll(
		int mapSeed,
		string contractId,
		string scope,
		EDangerLevel danger)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentException.ThrowIfNullOrEmpty(scope);

		var budget = EncounterPowerCatalog.EncounterPowerBudget(danger);
		var rng = new StableRandom(
			StableSeedMixer.From(mapSeed).Add(contractId).Add(scope).Value);
		var picked = PowerBudgetRoller.Spend(
			budget,
			_ => ShipPowerCatalog.All.ToArray(),
			level => level.Power,
			count => (int)(rng.NextDouble() * count),
			canonicalOrder: null);
		var ships = picked
			.Select(level => (level.Chassis, level.Tier))
			.ToList();

		if (ships.Count == 0)
			ships.Add((EType.RepurposedMiner, EShipGearTier.T0));

		return ships;
	}
}
