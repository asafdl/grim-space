using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi;

public abstract class PointOfInterest
{
	public string Id { get; }
	public string DisplayName { get; }
	public Coord? Center { get; }
	public int Radius { get; }
	public EPoiLogicalRole LogicalRole { get; }
	public int NextAvailableTaskTick { get; set; } = 1;

	protected PointOfInterest(
		string id,
		string displayName,
		int radius,
		EPoiLogicalRole logicalRole,
		Coord? center)
	{
		Id = id;
		DisplayName = displayName;
		Radius = radius;
		LogicalRole = logicalRole;
		Center = center;
	}

	public bool HasTasks => LogicalRole != EPoiLogicalRole.Environment;

	public bool HasDock => LogicalRole != EPoiLogicalRole.Environment;

	public virtual int RouteExclusionRadius => Radius + 6;

	public Coord PlacedCenter =>
		Center ?? throw new InvalidOperationException($"POI '{Id}' is not placed.");

	public abstract string DockNeighbourPoiId(SupplySystemPlan plan);

	public virtual int DurationTicks(EType unitType) =>
		throw new InvalidOperationException($"POI '{Id}' has no task for unit type {unitType}.");

	public (int StartTick, int EndTick) ReserveTaskWindow(int currentTick, int durationTicks)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationTicks);
		var startTick = System.Math.Max(currentTick, NextAvailableTaskTick);
		var endTick = startTick + durationTicks;
		NextAvailableTaskTick = endTick;
		return (startTick, endTick);
	}

	public void ExtendReservation(int endTick)
	{
		if (endTick > NextAvailableTaskTick)
			NextAvailableTaskTick = endTick;
	}

	public PointOfInterest Place(Coord center)
	{
		if (Center is not null)
			throw new InvalidOperationException($"POI '{Id}' is already placed.");

		return WithCenter(center);
	}

	public abstract PointOfInterest Fork();

	protected abstract PointOfInterest WithCenter(Coord center);

	protected void ForkReservationState(PointOfInterest clone) =>
		clone.NextAvailableTaskTick = NextAvailableTaskTick;
}
