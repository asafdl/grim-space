using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run.Persistence;

public sealed record ResourceBalanceDto(
	ResourceId Id,
	int Balance);
