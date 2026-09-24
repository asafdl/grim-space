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

	public sealed record LandmarkWithBorderTriangle(
		string LandmarkId,
		Coord BorderPointA,
		Coord BorderPointB) : AreaRelation;
}

public sealed record AreaPick(AreaIntel Intel, IReadOnlyList<Coord> SpawnPoints);
