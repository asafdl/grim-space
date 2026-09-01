using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class Star : PointOfInterest
{
	public const int DefaultRadius = 64;

	public static Star Template() => new(null);

	private Star(Coord? center) :
		base(
			SupplySystemPlan.StarPoiId,
			"Star",
			DefaultRadius,
			EPoiLogicalRole.Environment,
			center)
	{
	}

	public override int RouteExclusionRadius => Radius + 30;

	public override string DockNeighbourPoiId(SupplySystemPlan plan) =>
		throw new InvalidOperationException($"POI '{Id}' does not receive a dock.");

	public override PointOfInterest Fork()
	{
		var clone = new Star(Center);
		ForkReservationState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) => new Star(center);
}
