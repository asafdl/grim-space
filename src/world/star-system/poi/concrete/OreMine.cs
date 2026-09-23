using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class OreMine : PointOfInterest
{
	public const int DefaultRadius = 32;
	public const string MineFacilitySlug = "mine";
	public const string MineScenePath = "res://scenes/copper_mine.tscn";
	public const string MineContractOperatorSceneSlotId = "Manager";

	private readonly SupplySystemPlan _plan;

	public static OreMine Template(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
		new(plan, null, BuildFacilities(plan, operatorNames));

	public static IReadOnlyList<Facility> BuildFacilities(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
	[
		new Facility(
			Facility.ScopedId(plan.ExtractionPoiId, MineFacilitySlug),
			"Mine",
			EPresentationAnchor.Mine,
			MineScenePath,
			[
				new FacilityOperator(
					operatorNames.Take(),
					EFacilityOperatorRole.Contracts,
					MineContractOperatorSceneSlotId),
			]),
	];

	private OreMine(SupplySystemPlan plan, Coord? center, IReadOnlyList<Facility> facilities) :
		base(
			plan.ExtractionPoiId,
			DisplayNameFor(plan),
			DefaultRadius,
			EPoiLogicalRole.Extraction,
			center,
			facilities: facilities)
	{
		_plan = plan;
	}

	public override string DockNeighbourPoiId(SupplySystemPlan plan) => plan.RefineryPoiId;

	public override int DurationTicks(EType unitType) =>
		unitType switch
		{
			EType.MiningBarge => 8,
			EType.ComplianceVessel => 4,
			EType.CargoShuttle => 3,
			EType.ServiceVessel => 4,
			_ => throw new InvalidOperationException(
				$"Extraction POI has no task for unit type {unitType}."),
		};

	public override PointOfInterest Fork()
	{
		var clone = new OreMine(_plan, Center, Facilities);
		ForkReservationState(clone);
		ForkFacadeState(clone);
		ForkFacilityState(clone);
		ForkOperatorTemporaryRoles(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) =>
		new OreMine(_plan, center, Facilities);

	private static string DisplayNameFor(SupplySystemPlan plan) =>
		$"{char.ToUpperInvariant(plan.ResourceId[0])}{plan.ResourceId[1..]} Field";
}
