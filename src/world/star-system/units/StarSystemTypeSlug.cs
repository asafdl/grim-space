namespace GrimSpace.World.StarSystem.Units;

public static class StarSystemTypeSlug
{
	public static string For(EType type) =>
		type switch
		{
			EType.CargoShuttle => "cargo-shuttle",
			EType.ServiceVessel => "service-vessel",
			EType.Patrol => "patrol",
			EType.MiningBarge => "mining-barge",
			EType.RefineryHauler => "refinery-hauler",
			EType.ExportFreighter => "export-freighter",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown star-system unit type."),
		};
}
