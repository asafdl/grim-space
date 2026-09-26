using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts.Encounter;

public readonly record struct EncounterCatalogEntry(EType Chassis, EShipGearTier Tier, int Power);

public static class EncounterPowerCatalog
{
	private static readonly EncounterCatalogEntry[] Entries =
	[
		new(EType.Patrol, EShipGearTier.T0, 1),
		new(EType.Patrol, EShipGearTier.T1, 2),
		new(EType.Patrol, EShipGearTier.T2, 4),
		new(EType.Patrol, EShipGearTier.T3, 8),
		new(EType.Carrier, EShipGearTier.T0, 11),
	];

	public static int EncounterPowerBudget(EDangerLevel danger) =>
		danger switch
		{
			EDangerLevel.VeryLow => 1,
			EDangerLevel.Low => 2,
			EDangerLevel.Moderate => 7,
			EDangerLevel.High => 12,
			EDangerLevel.VeryHigh => 16,
			_ => throw new ArgumentOutOfRangeException(nameof(danger), danger, null),
		};

	public static IEnumerable<EncounterCatalogEntry> AllEntries => Entries;

	public static IEnumerable<EncounterCatalogEntry> Affordable(int remainingPower) =>
		Entries.Where(entry => entry.Power <= remainingPower);
}
