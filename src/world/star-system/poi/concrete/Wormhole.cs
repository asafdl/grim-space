using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class Wormhole : PointOfInterest
{
	public const int DefaultRadius = 32;

	private readonly SupplySystemPlan _plan;

	public static Wormhole Template(SupplySystemPlan plan) => new(plan, null);

	private Wormhole(SupplySystemPlan plan, Coord? center) :
		base(
			plan.ExitPoiId,
			"Exit",
			DefaultRadius,
			EPoiLogicalRole.Exit,
			center)
	{
		_plan = plan;
	}

	public override string DockNeighbourPoiId(SupplySystemPlan plan) => plan.StoragePoiId;

	public override int DurationTicks(EType unitType) =>
		unitType switch
		{
			EType.ExportFreighter => 10,
			EType.ComplianceVessel => 4,
			_ => throw new InvalidOperationException(
				$"Exit POI has no task for unit type {unitType}."),
		};

	public override PointOfInterest Fork()
	{
		var clone = new Wormhole(_plan, Center);
		ForkTaskState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) => new Wormhole(_plan, center);
}
