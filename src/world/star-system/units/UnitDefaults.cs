namespace GrimSpace.World.StarSystem.Units;

public static class UnitDefaults
{
	public static double SpeedPerTick(EType type) =>
		type switch
		{
			EType.MiningBarge => 10,
			EType.RefineryHauler => 12,
			EType.ExportFreighter => 10,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "No default speed for unit type."),
		};

	public static int WorkDuration(EType type) =>
		type switch
		{
			EType.MiningBarge => 2,
			EType.RefineryHauler => 2,
			EType.ExportFreighter => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "No default work duration for unit type."),
		};
}
