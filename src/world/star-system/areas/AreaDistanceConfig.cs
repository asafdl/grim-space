namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaDistanceConfig(
	double LowFraction = 0.10,
	double MedMinFraction = 0.15,
	double MedMaxFraction = 0.35,
	double HighMinFraction = 0.40);
