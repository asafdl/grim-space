using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class StorageFacility : PointOfInterest
{
	public const int DefaultRadius = 32;

	private readonly SupplySystemPlan _plan;

	public static StorageFacility Template(SupplySystemPlan plan) => new(plan, null);

	private StorageFacility(SupplySystemPlan plan, Coord? center) :
		base(
			plan.StoragePoiId,
			"Storage",
			DefaultRadius,
			EPoiLogicalRole.Storage,
			center)
	{
		_plan = plan;
	}

	public override string DockNeighbourPoiId(SupplySystemPlan plan) => plan.ExitPoiId;

	public override int DurationTicks(EType unitType) =>
		unitType switch
		{
			EType.RefineryHauler => 5,
			EType.ExportFreighter => 7,
			EType.ComplianceVessel => 4,
			EType.CargoShuttle => 3,
			EType.ServiceVessel => 4,
			_ => throw new InvalidOperationException(
				$"Storage POI has no task for unit type {unitType}."),
		};

	public override PointOfInterest Fork()
	{
		var clone = new StorageFacility(_plan, Center);
		ForkReservationState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) => new StorageFacility(_plan, center);
}
