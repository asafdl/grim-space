namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaIntelContext(
	string LandmarkADisplayName,
	string LandmarkBDisplayName,
	EAreaDistance Distance);
