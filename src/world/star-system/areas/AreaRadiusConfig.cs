namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaRadiusConfig(
	int MinRadius = 1,
	int MaxRadius = 16,
	double FractionOfSpan = 0.30);
