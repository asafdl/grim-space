using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class TradeHub : PointOfInterest
{
	public const int DefaultRadius = 34;

	private readonly SupplySystemPlan _plan;

	public static TradeHub Template(SupplySystemPlan plan) => new(plan, null);

	private TradeHub(SupplySystemPlan plan, Coord? center) :
		base(
			plan.TradeHubPoiId,
			"Trade Hub",
			DefaultRadius,
			EPoiLogicalRole.Trade,
			center)
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
		var clone = new TradeHub(_plan, Center);
		ForkReservationState(clone);
		ForkFacadeState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) => new TradeHub(_plan, center);
}
