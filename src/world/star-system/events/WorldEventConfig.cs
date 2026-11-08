using GrimSpace.World.StarSystem.Economy;

namespace GrimSpace.World.StarSystem.Events;

public static class WorldEventConfig
{
	public const int WakeCadenceMinutes = 6;
	public const int WakeCadenceTicks =
		StrategicEconomyConfig.StrategicTicksPerSecond * 60 * WakeCadenceMinutes;
	public const int TargetScheduledRolls = 2;
}
