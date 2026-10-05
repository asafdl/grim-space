namespace GrimSpace.World.StarSystem.FleetSpawner;

public sealed class FleetSpawnerConfig
{
	public const int DefaultCadenceTicks = 5;
	public const int DefaultTtlTicks = 300;
	public const int DefaultRandomAreaTargetCount = 3;

	public int CadenceTicks { get; init; } = DefaultCadenceTicks;
	public int TtlTicks { get; init; } = DefaultTtlTicks;
	public int RandomAreaTargetCount { get; init; } = DefaultRandomAreaTargetCount;
}
