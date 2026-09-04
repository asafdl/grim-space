using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class AdministrativeCore : PointOfInterest
{
	public const int DefaultRadius = 36;

	private readonly SupplySystemPlan _plan;

	public EPoiPhysicalForm PhysicalForm { get; }

	public static AdministrativeCore Template(SupplySystemPlan plan, int seed)
	{
		var random = new StableRandom(StableSeedMixer.From(seed).Add("admin-core-form").Value);
		var form = random.NextDouble() < 0.7
			? EPoiPhysicalForm.Planet
			: EPoiPhysicalForm.LargeStation;
		return new AdministrativeCore(plan, null, form);
	}

	private static IReadOnlyList<Facility> DefaultFacilities(SupplySystemPlan plan) =>
	[
		new Facility(
			Facility.ScopedId(plan.AdministrativePoiId, "management"),
			"Command Authority",
			EPresentationAnchor.Management,
			[EServiceKind.Contracts]),
	];

	private AdministrativeCore(SupplySystemPlan plan, Coord? center, EPoiPhysicalForm physicalForm) :
		base(
			plan.AdministrativePoiId,
			"Administrative Core",
			DefaultRadius,
			EPoiLogicalRole.Administrative,
			center,
			physicalForm == EPoiPhysicalForm.Planet ? PoiFacade.Planet : PoiFacade.LargeStation,
			DefaultFacilities(plan))
	{
		_plan = plan;
		PhysicalForm = physicalForm;
	}

	public override string DockNeighbourPoiId(SupplySystemPlan plan) => plan.RefineryPoiId;

	public override int DurationTicks(EType unitType) =>
		unitType switch
		{
			EType.ComplianceVessel => 8,
			EType.CargoShuttle => 3,
			EType.ServiceVessel => 4,
			_ => throw new InvalidOperationException(
				$"Administrative Core POI has no task for unit type {unitType}."),
		};

	public override PointOfInterest Fork()
	{
		var clone = new AdministrativeCore(_plan, Center, PhysicalForm);
		ForkReservationState(clone);
		ForkFacadeState(clone);
		ForkFacilityState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) =>
		new AdministrativeCore(_plan, center, PhysicalForm);
}
