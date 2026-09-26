using GrimSpace.Math;
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
		var ships = new List<(EType Chassis, EShipGearTier GearTier)>();
		var remaining = budget;

		while (remaining > 0)
		{
			var affordable = EncounterPowerCatalog.Affordable(remaining).ToArray();
			if (affordable.Length == 0)
				break;

			var pick = affordable[(int)(rng.NextDouble() * affordable.Length)];
			ships.Add((pick.Chassis, pick.Tier));
			remaining -= pick.Power;
		}

		if (ships.Count == 0)
			ships.Add((EType.Patrol, EShipGearTier.T0));

		return ships;
	}
}
