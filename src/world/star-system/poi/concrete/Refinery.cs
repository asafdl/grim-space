using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class Refinery : PointOfInterest
{
	public const int DefaultRadius = 32;
	public const string RefineryFacilitySlug = "refinery";
	public const string RefineryScenePath = "res://scenes/refinery.tscn";
	public const string RefineryOperatorSceneSlotId = "WeirdDude";

	private readonly SupplySystemPlan _plan;

	public static Refinery Template(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
		new(plan, null, BuildFacilities(plan, operatorNames));

	public static IReadOnlyList<Facility> BuildFacilities(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
	[
		new Facility(
			Facility.ScopedId(plan.RefineryPoiId, RefineryFacilitySlug),
			"Refinery",
			EPresentationAnchor.Refinery,
			RefineryScenePath,
			[
				new FacilityOperator(
					operatorNames.Take(),
					EFacilityOperatorRole.Dialog,
					RefineryOperatorSceneSlotId),
			]),
	];

	private Refinery(SupplySystemPlan plan, Coord? center, IReadOnlyList<Facility> facilities) :
		base(
			plan.RefineryPoiId,
			"Refinery",
			DefaultRadius,
			EPoiLogicalRole.Refinery,
			center,
			facilities: facilities)
	{
		_plan = plan;
	}

	public override string DockNeighbourPoiId(SupplySystemPlan plan) => plan.StoragePoiId;

	public override int DurationTicks(EType unitType) =>
		unitType switch
		{
			EType.MiningBarge => 5,
			EType.RefineryHauler => 6,
			EType.ComplianceVessel => 4,
			EType.CargoShuttle => 3,
			EType.ServiceVessel => 4,
			_ => throw new InvalidOperationException(
				$"Refinery POI has no task for unit type {unitType}."),
		};

	public override PointOfInterest Fork()
	{
		var clone = new Refinery(_plan, Center, Facilities);
		ForkReservationState(clone);
		ForkFacadeState(clone);
		ForkFacilityState(clone);
		ForkOperatorTemporaryRoles(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) =>
		new Refinery(_plan, center, Facilities);
}
