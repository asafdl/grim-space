using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaIntel(
	string Template,
	string LandmarkAId,
	string LandmarkBId,
	string LandmarkCId);

public abstract record AreaRelation
{
	public sealed record TriangulatedLandmarks(
		string LandmarkAId,
		string LandmarkBId,
		string LandmarkCId) : AreaRelation;
}

public sealed record AreaPick(
	Coord Center,
	int Radius,
	AreaIntel Intel,
	AreaRelation Relation);
