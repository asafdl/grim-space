using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Poi;

public abstract class PointOfInterest
{
	private readonly Queue<string>? _waiting;
	private string? _activeUnitId;

	public string Id { get; }
	public string DisplayName { get; }
	public Coord? Center { get; }
	public int Radius { get; }
	public EPoiLogicalRole LogicalRole { get; }

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
		if (HasTasks)
			_waiting = new Queue<string>();
	}

	public bool HasTasks => LogicalRole != EPoiLogicalRole.Environment;

	public bool HasDock => LogicalRole != EPoiLogicalRole.Environment;

	public virtual int RouteExclusionRadius => Radius + 6;

	public Coord PlacedCenter =>
		Center ?? throw new InvalidOperationException($"POI '{Id}' is not placed.");

	public abstract string DockNeighbourPoiId(SupplySystemPlan plan);

	public virtual int DurationTicks(EType unitType) =>
		throw new InvalidOperationException($"POI '{Id}' has no task for unit type {unitType}.");

	public void Enqueue(string unitId)
	{
		if (!HasTasks)
			throw new InvalidOperationException($"POI '{Id}' does not accept tasks.");

		ArgumentException.ThrowIfNullOrEmpty(unitId);
		_waiting!.Enqueue(unitId);
	}

	public void AdoptSpawnedWorker(string unitId)
	{
		if (!HasTasks)
			throw new InvalidOperationException($"POI '{Id}' does not accept tasks.");

		ArgumentException.ThrowIfNullOrEmpty(unitId);
		if (_activeUnitId is not null)
			throw new InvalidOperationException($"POI '{Id}' already has an active worker.");

		_activeUnitId = unitId;
	}

	public void AdvanceTick(UnitRegistry units)
	{
		if (!HasTasks)
			return;

		if (_activeUnitId is not null)
		{
			var active = units.UnitOf(_activeUnitId).State;
			if (active.Phase == EPhase.Working)
			{
				active.TickWork();
				if (active.Phase == EPhase.Docked)
					_activeUnitId = null;
			}
			else
			{
				_activeUnitId = null;
			}
		}

		if (_activeUnitId is not null || _waiting!.Count == 0)
			return;

		var nextId = _waiting.Dequeue();
		var next = units.UnitOf(nextId).State;
		next.BeginWork(DurationTicks(next.Type));
		_activeUnitId = nextId;
	}

	public PointOfInterest Place(Coord center)
	{
		if (Center is not null)
			throw new InvalidOperationException($"POI '{Id}' is already placed.");

		return WithCenter(center);
	}

	public abstract PointOfInterest Fork();

	protected abstract PointOfInterest WithCenter(Coord center);

	protected void ForkTaskState(PointOfInterest clone)
	{
		if (_waiting is null || clone._waiting is null)
			return;

		foreach (var unitId in _waiting)
			clone._waiting.Enqueue(unitId);
		clone._activeUnitId = _activeUnitId;
	}
}
