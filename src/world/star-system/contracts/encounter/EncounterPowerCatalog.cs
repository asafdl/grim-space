using GrimSpace.World.StarSystem.Encounter;

namespace GrimSpace.World.StarSystem.Contracts.Encounter;

/// <summary>
/// Maps contract danger to encounter power budget. Ship chassis/tier costs live in <see cref="Units.ShipPowerCatalog"/>.
/// </summary>
public static class EncounterPowerCatalog
{
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
}
