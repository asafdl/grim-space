using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class Wormhole : PointOfInterest
{
	public const int DefaultRadius = 32;
	public const string TravelFacilitySlug = "travel";
	public const string TravelScenePath = "res://scenes/travel.tscn";
	public const string TravelOperatorSceneSlotId = "WeirdDude";

	private readonly SupplySystemPlan _plan;

	public static Wormhole Template(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
		new(plan, null, BuildFacilities(plan, operatorNames));

	public static IReadOnlyList<Facility> BuildFacilities(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
	[
		new Facility(
			Facility.ScopedId(plan.ExitPoiId, TravelFacilitySlug),
			"Travel",
			EPresentationAnchor.Travel,
			TravelScenePath,
			[
				new FacilityOperator(
					operatorNames.Take(),
					EFacilityOperatorRole.Dialog,
					TravelOperatorSceneSlotId),
			]),
	];

	private Wormhole(SupplySystemPlan plan, Coord? center, IReadOnlyList<Facility> facilities) :
		base(
			plan.ExitPoiId,
			"Exit",
			DefaultRadius,
			EPoiLogicalRole.Exit,
			center,
			facilities: facilities)
	{
		_plan = plan;
	}

	public override string DockNeighbourPoiId(SupplySystemPlan plan) => plan.StoragePoiId;

	public override int DurationTicks(EType unitType) =>
		unitType switch
		{
			EType.ExportFreighter => 10,
			EType.ComplianceVessel => 4,
			EType.CargoShuttle => 3,
			EType.ServiceVessel => 4,
			_ => throw new InvalidOperationException(
				$"Exit POI has no task for unit type {unitType}."),
		};

	public override PointOfInterest Fork()
	{
		var clone = new Wormhole(_plan, Center, Facilities);
		ForkReservationState(clone);
		ForkFacadeState(clone);
		ForkFacilityState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) =>
		new Wormhole(_plan, center, Facilities);
}
