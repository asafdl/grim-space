namespace GrimSpace.World.StarSystem.Units;

public static class UnitDefaults
{
	public static double SpeedPerTick(EType type) =>
		type switch
		{
			EType.MiningBarge => 5,
			EType.RefineryHauler => 6,
			EType.ExportFreighter => 5,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "No default speed for unit type."),
		};
}
