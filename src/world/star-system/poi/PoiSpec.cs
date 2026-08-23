using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Poi;

public sealed record PoiSpec(
	string Id,
	EPointOfInterestKind Kind,
	EPoiLogicalRole LogicalRole,
	string DisplayName,
	int Radius)
{
	public PointOfInterest Place(Coord center) =>
		new(Id, Kind, DisplayName, center, Radius, LogicalRole);
}
