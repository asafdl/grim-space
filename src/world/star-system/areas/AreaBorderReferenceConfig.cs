namespace GrimSpace.World.StarSystem.Areas;

/// <summary>
/// Rim hunt placement. The only value that usually needs tuning is how far from a
/// sector edge the landmark anchor may sit (as a fraction of map diagonal).
/// </summary>
public sealed record AreaBorderReferenceConfig(
	double MaxDistanceFromBorderFractionOfMapDiagonal = 0.12);
