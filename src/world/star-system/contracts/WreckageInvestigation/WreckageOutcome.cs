using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Contracts.Objectives;

public abstract record WreckageOutcome
{
	public sealed record Salvage(ResourceBundle Loot) : WreckageOutcome;

	public sealed record Ambush(FleetSpawnSpec Fleet) : WreckageOutcome;
}
