using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Presentation.Map;

namespace GrimSpace.World.StarSystem.Presentation.Picking;

public enum MapInteractiveTargetKind
{
	None,
	SelfClickNoOp,
	Unit,
	Wreck,
	Landmark,
	Dock,
	Poi,
}

public readonly record struct MapInteractiveTarget(
	MapInteractiveTargetKind Kind,
	UnitsView.UnitHoverInfo? Unit,
	string? WreckContractId,
	string? LandmarkId,
	MapView.DockHoverInfo? Dock,
	string? PoiId,
	Coord? MoveGridPoint)
{
	public static MapInteractiveTarget SelfClick() =>
		new(MapInteractiveTargetKind.SelfClickNoOp, null, null, null, null, null, null);

	public static MapInteractiveTarget Move(Coord grid) =>
		new(MapInteractiveTargetKind.None, null, null, null, null, null, grid);
}
