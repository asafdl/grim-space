namespace GrimSpace.World.StarSystem.Areas;

public abstract record AreaRelation
{
	public sealed record BetweenLandmarks(
		string LandmarkAId,
		string LandmarkBId,
		EAreaDistance Distance) : AreaRelation;
}
