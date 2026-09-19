using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaPick(
	Coord Center,
	int Radius,
	AreaIntel Intel,
	AreaRelation Relation);
