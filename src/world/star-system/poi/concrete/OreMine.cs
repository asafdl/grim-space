using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi.Concrete;

public sealed class OreMine : PointOfInterest
{
	public const int DefaultRadius = 32;

	private readonly SupplySystemPlan _plan;

	public static OreMine Template(SupplySystemPlan plan) => new(plan, null);

	private OreMine(SupplySystemPlan plan, Coord? center) :
		base(
			plan.ExtractionPoiId,
			DisplayNameFor(plan),
			DefaultRadius,
			EPoiLogicalRole.Extraction,
			center)
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
		var clone = new OreMine(_plan, Center);
		ForkReservationState(clone);
		return clone;
	}

	protected override PointOfInterest WithCenter(Coord center) => new OreMine(_plan, center);

	private static string DisplayNameFor(SupplySystemPlan plan) =>
		$"{char.ToUpperInvariant(plan.ResourceId[0])}{plan.ResourceId[1..]} Field";
}
