using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

internal sealed class ContactMonitor
{
	private const int MaxCheckBackoffTicks = 64;

	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly IPathfinder _pathfinder;
	private readonly Dictionary<(string InitiatorId, string TargetId), ContactWatch> _watches = [];

	public ContactMonitor(Engine<StarMap, ActorRuntime> engine, IPathfinder pathfinder)
	{
		_engine = engine;
		_pathfinder = pathfinder;
	}

	private StarMap Map => _engine.World;

	public Coord CommittedPositionOf(string unitId, float tickFraction = 0f)
	{
		var unit = Map.UnitRegistry.UnitOf(unitId);
		var runtime = _engine.ActorRuntimes.For(unitId);
		TransitCache.RebuildIfMissing(unit, runtime, _pathfinder);
		return unit.State.CommittedPosition(Map, runtime.CachedPath, tickFraction).Position;
	}

	public IReadOnlyList<IAction<StarMap, ActorRuntime>> Update(int currentTick)
	{
		ReconcileWatches(currentTick);

		var produced = new List<IAction<StarMap, ActorRuntime>>();
		foreach (var watch in _watches.Values.ToList())
		{
			if (watch.NextCheckTick > currentTick)
				continue;

			if (TryProduceReachContact(watch, currentTick, out var action))
				produced.Add(action);
		}

		return produced;
	}

	private sealed class ContactWatch
	{
		public required string InitiatorId { get; init; }
		public required string TargetId { get; init; }
		public int NextCheckTick { get; set; }
	}

	private void ReconcileWatches(int currentTick)
	{
		var activePursuits = CollectActivePursuits();

		foreach (var key in _watches.Keys.ToList())
		{
			if (!activePursuits.Contains(key))
				_watches.Remove(key);
		}

		foreach (var (initiatorId, targetId) in activePursuits)
		{
			if (_watches.ContainsKey((initiatorId, targetId)))
				continue;

			_watches[(initiatorId, targetId)] = new ContactWatch
			{
				InitiatorId = initiatorId,
				TargetId = targetId,
				NextCheckTick = currentTick,
			};
		}
	}

	private HashSet<(string InitiatorId, string TargetId)> CollectActivePursuits()
	{
		var pursuits = new HashSet<(string, string)>();
		foreach (var unit in Map.UnitRegistry.All)
		{
			var state = unit.State;
			if (state.EngagementPhase != EEngagementPhase.Pursuing
				|| state.EngagementTargetUnitId is not { } targetId
				|| state.EngagedWithUnitIds.Count > 0)
				continue;

			if (!Map.UnitRegistry.TryGet(targetId, out _))
				continue;

			pursuits.Add((unit.State.Id, targetId));
		}

		return pursuits;
	}

	private bool TryProduceReachContact(
		ContactWatch watch,
		int currentTick,
		out IAction<StarMap, ActorRuntime> action)
	{
		action = null!;
		var key = (watch.InitiatorId, watch.TargetId);
		if (!Map.UnitRegistry.TryGet(watch.InitiatorId, out var initiator))
		{
			_watches.Remove(key);
			return false;
		}

		var state = initiator.State;
		if (state.EngagementPhase != EEngagementPhase.Pursuing
			|| state.EngagementTargetUnitId != watch.TargetId)
		{
			_watches.Remove(key);
			return false;
		}

		if (!Map.UnitRegistry.TryGet(watch.TargetId, out _))
		{
			_watches.Remove(key);
			return false;
		}

		if (!EngagementQueries.IsHunterInEngageRange(
				Map,
				watch.InitiatorId,
				watch.TargetId,
				id => CommittedPositionOf(id)))
		{
			RescheduleWatch(watch, currentTick);
			return false;
		}

		_watches.Remove(key);
		action = new ReachContactAction(watch.InitiatorId, watch.TargetId);
		return true;
	}

	private void RescheduleWatch(ContactWatch watch, int currentTick)
	{
		var initiator = Map.UnitRegistry.UnitOf(watch.InitiatorId);
		var target = Map.UnitRegistry.UnitOf(watch.TargetId);
		var initiatorPosition = CommittedPositionOf(watch.InitiatorId);
		var targetPosition = CommittedPositionOf(watch.TargetId);
		var dx = initiatorPosition.X - targetPosition.X;
		var dz = initiatorPosition.Z - targetPosition.Z;
		var distance = System.Math.Sqrt(dx * dx + dz * dz);
		var gap = System.Math.Max(0, distance - initiator.State.EngageRadius);
		var maxClosingSpeed = (initiator.State.SpeedPerTick + target.State.SpeedPerTick)
			* PathfindingCell.RouteSpeedCeiling;
		var delay = maxClosingSpeed <= 0
			? MaxCheckBackoffTicks
			: (int)System.Math.Clamp(System.Math.Floor(gap / maxClosingSpeed), 1, MaxCheckBackoffTicks);
		watch.NextCheckTick = currentTick + delay;
	}
}
