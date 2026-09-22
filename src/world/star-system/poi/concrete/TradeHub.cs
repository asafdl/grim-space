using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class TradeHub : PointOfInterest
{
	public const int DefaultRadius = 34;
	public const string DockyardFacilitySlug = "dockyard";
	public const string DockyardScenePath = "res://scenes/dockyard.tscn";
	public const string ShopOperatorSceneSlotId = "Salesman";
	public const string ShieldOperatorSceneSlotId = "ShieldRecharge";
	public const string MarketFacilitySlug = "market";
	public const string MarketScenePath = "res://scenes/market.tscn";
	public const string MarketOperatorSceneSlotId = "WeirdDude";

	private readonly SupplySystemPlan _plan;

	public static TradeHub Template(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
		new(plan, null, BuildFacilities(plan, operatorNames));

	public static IReadOnlyList<Facility> BuildFacilities(SupplySystemPlan plan, OperatorNameAllocator operatorNames) =>
	[
		new Facility(
			Facility.ScopedId(plan.TradeHubPoiId, DockyardFacilitySlug),
			"Dockyard",
			EPresentationAnchor.Dockyard,
			DockyardScenePath,
			[
				new FacilityOperator(
					operatorNames.Take(),
					EFacilityOperatorRole.DockyardShop,
					ShopOperatorSceneSlotId),
				new FacilityOperator(
					operatorNames.Take(),
					EFacilityOperatorRole.ShieldRecharge,
					ShieldOperatorSceneSlotId),
			]),
		new Facility(
			Facility.ScopedId(plan.TradeHubPoiId, MarketFacilitySlug),
			"Market",
			EPresentationAnchor.Market,
			MarketScenePath,
			[
				new FacilityOperator(
					operatorNames.Take(),
					EFacilityOperatorRole.Dialog,
					MarketOperatorSceneSlotId),
			]),
	];

	private TradeHub(SupplySystemPlan plan, Coord? center, IReadOnlyList<Facility> facilities) :
		base(
			plan.TradeHubPoiId,
			"Trade Hub",
			DefaultRadius,
			EPoiLogicalRole.Trade,
			center,
			facilities: facilities)
	{
		_plan = plan;
	}

	public override string DockNeighbourPoiId(SupplySystemPlan plan) => plan.RefineryPoiId;

	public override int DurationTicks(EType unitType) =>
		unitType switch
		{
			EType.CargoShuttle => 10,
			EType.ServiceVessel => 8,
			_ => throw new InvalidOperationException(
				$"Trade Hub POI has no task for unit type {unitType}."),
		};

	public override PointOfInterest Fork()
	{
		var clone = new TradeHub(_plan, Center, Facilities);
		ForkReservationState(clone);
		ForkFacadeState(clone);
		ForkFacilityState(clone);
		ForkOperatorTemporaryRoles(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) => new TradeHub(_plan, center, Facilities);
}
