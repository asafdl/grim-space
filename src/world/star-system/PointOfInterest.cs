using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem;

public sealed record PointOfInterest(
	string Id,
	EPointOfInterestKind Kind,
	string DisplayName,
	Coord Center,
	int Radius);
