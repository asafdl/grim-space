using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class AdministrativeCore : PointOfInterest
{
	public const int DefaultRadius = 36;
	public const string ManagementFacilitySlug = "management";
	public const string ManagementScenePath = "res://scenes/command_authority.tscn";
	public const string ContractOperatorSceneSlotId = "Manager";

	private readonly SupplySystemPlan _plan;

	public EPoiPhysicalForm PhysicalForm { get; }

	public static AdministrativeCore Template(SupplySystemPlan plan, int seed, OperatorNameAllocator operatorNames)
	{
		var random = new StableRandom(StableSeedMixer.From(seed).Add("admin-core-form").Value);
		var form = random.NextDouble() < 0.7
			? EPoiPhysicalForm.Planet
			: EPoiPhysicalForm.LargeStation;
		return new AdministrativeCore(
			plan,
			null,
			form,
			BuildFacilities(plan, operatorNames));
	}

	public static IReadOnlyList<Facility> BuildFacilities(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
	[
		new Facility(
			Facility.ScopedId(plan.AdministrativePoiId, ManagementFacilitySlug),
			"Command Authority",
			EPresentationAnchor.Management,
			ManagementScenePath,
			[
				new FacilityOperator(
					operatorNames.Take(),
					EFacilityOperatorRole.Contracts,
					ContractOperatorSceneSlotId),
			]),
	];

	private AdministrativeCore(
		SupplySystemPlan plan,
		Coord? center,
		EPoiPhysicalForm physicalForm,
		IReadOnlyList<Facility> facilities) :
		base(
			plan.AdministrativePoiId,
			"Administrative Core",
			DefaultRadius,
			EPoiLogicalRole.Administrative,
			center,
			physicalForm == EPoiPhysicalForm.Planet ? PoiFacade.Planet : PoiFacade.LargeStation,
			facilities)
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
		var clone = new AdministrativeCore(_plan, Center, PhysicalForm, Facilities);
		ForkReservationState(clone);
		ForkFacadeState(clone);
		ForkFacilityState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) =>
		new AdministrativeCore(_plan, center, PhysicalForm, Facilities);
}
