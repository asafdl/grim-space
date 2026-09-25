using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

internal sealed class ContactMonitor
{
	private const int MaxCheckBackoffTicks = 64;

	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly IPathfinder _pathfinder;
	private readonly Dictionary<string, ContactWatch> _watches = [];

	public ContactMonitor(Engine<StarMap, ActorRuntime> engine, IPathfinder pathfinder)
	{
		_engine = engine;
		_pathfinder = pathfinder;
	}

	private StarMap Map => _engine.World;

	public Coord CommittedPositionOf(string unitId, float tickFraction = 0f)
	{
		var unit = Map.FleetRegistry.FleetOf(unitId);
		var runtime = _engine.ActorRuntimes.For(unitId);
		TransitCache.RebuildIfMissing(unit, runtime, _pathfinder);
		return unit.State.CommittedPosition(Map, runtime.CachedPath, tickFraction).Position;
	}

	public IReadOnlyList<ContactReached> Update(int currentTick)
	{
		ReconcileWatches(currentTick);

		var produced = new List<ContactReached>();
		foreach (var watch in _watches.Values.ToList())
		{
			if (watch.NextCheckTick > currentTick)
				continue;

			if (TryProduceContact(watch, currentTick, out var reached))
				produced.Add(reached);
		}

		return produced;
	}

	private sealed class ContactWatch
	{
		public required string ActorId { get; init; }
		public required TravelTarget TravelTarget { get; init; }
		public int NextCheckTick { get; set; }
	}

	private void ReconcileWatches(int currentTick)
	{
		var activeTargets = CollectActiveTravelTargets();

		foreach (var actorId in _watches.Keys.ToList())
		{
			if (!activeTargets.ContainsKey(actorId))
				_watches.Remove(actorId);
		}

		foreach (var (actorId, travelTarget) in activeTargets)
		{
			if (_watches.TryGetValue(actorId, out var existing)
				&& existing.TravelTarget == travelTarget)
				continue;

			_watches[actorId] = new ContactWatch
			{
				ActorId = actorId,
				TravelTarget = travelTarget,
				NextCheckTick = currentTick,
			};
		}
	}

	private Dictionary<string, TravelTarget> CollectActiveTravelTargets()
	{
		var targets = new Dictionary<string, TravelTarget>(StringComparer.Ordinal);
		foreach (var unit in Map.FleetRegistry.All)
		{
			var travelTarget = unit.State.TravelTarget;
			if (!travelTarget.IsActive || EngagementState.IsEngaged(unit.State))
				continue;

			if (travelTarget.Kind == ETravelTargetKind.Fleet
				&& !Map.FleetRegistry.TryGet(travelTarget.TargetId, out _))
				continue;

			if (travelTarget.Kind == ETravelTargetKind.Wreck
				&& !IsWreckTargetStillValid(unit.State.Id, travelTarget.TargetId))
				continue;

			targets[unit.State.Id] = travelTarget;
		}

		return targets;
	}

	private bool IsWreckTargetStillValid(string actorId, string contractId) =>
		Map.ContractRegistry.TryGet(contractId, out var contract)
		&& Map.ContractRegistry.TryGetState(contractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state.HolderUnitId == actorId
		&& !ContractFactory.IsWreckageObjectiveMet(contractId, Map, actorId)
		&& contract.Objective is WreckageObjective;

	private bool TryProduceContact(ContactWatch watch, int currentTick, out ContactReached reached)
	{
		reached = null!;
		if (!Map.FleetRegistry.TryGet(watch.ActorId, out var initiator))
		{
			_watches.Remove(watch.ActorId);
			return false;
		}

		var state = initiator.State;
		if (state.TravelTarget != watch.TravelTarget)
		{
			_watches.Remove(watch.ActorId);
			return false;
		}

		switch (watch.TravelTarget.Kind)
		{
			case ETravelTargetKind.Fleet:
				return TryProduceFleetContact(watch, currentTick, out reached);
			case ETravelTargetKind.Wreck:
				return TryProduceWreckContact(watch, currentTick, out reached);
			default:
				_watches.Remove(watch.ActorId);
				return false;
		}
	}

	private bool TryProduceFleetContact(ContactWatch watch, int currentTick, out ContactReached reached)
	{
		reached = null!;
		var targetId = watch.TravelTarget.TargetId;
		if (watch.ActorId == targetId
			|| stateIsInvalidForFleetPursuit(watch.ActorId, targetId))
		{
			_watches.Remove(watch.ActorId);
			return false;
		}

		if (!EngagementQueries.IsHunterInEngageRange(
				Map,
				watch.ActorId,
				targetId,
				id => CommittedPositionOf(id)))
		{
			RescheduleFleetWatch(watch, currentTick, targetId);
			return false;
		}

		_watches.Remove(watch.ActorId);
		reached = new ContactReached(watch.ActorId, new FleetContactTarget(targetId));
		return true;

		bool stateIsInvalidForFleetPursuit(string actorId, string fleetTargetId)
		{
			if (!Map.FleetRegistry.TryGet(actorId, out var actor))
				return true;

			var actorState = actor.State;
			return actorState.CurrentEngagement?.Phase != EEngagementPhase.Pursuing
				|| actorState.CurrentEngagement?.Hunting != fleetTargetId
				|| !Map.FleetRegistry.TryGet(fleetTargetId, out _);
		}
	}

	private bool TryProduceWreckContact(ContactWatch watch, int currentTick, out ContactReached reached)
	{
		reached = null!;
		var contractId = watch.TravelTarget.TargetId;
		if (!IsWreckTargetStillValid(watch.ActorId, contractId)
			|| !Map.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not WreckageObjective wreckage)
		{
			_watches.Remove(watch.ActorId);
			return false;
		}

		if (!Map.FleetRegistry.TryGet(watch.ActorId, out var unit))
		{
			_watches.Remove(watch.ActorId);
			return false;
		}

		var actorPosition = CommittedPositionOf(watch.ActorId);
		if (!EngagementQueries.IsHunterInEngageRange(
				actorPosition,
				wreckage.Position,
				unit.State.EngageRadius))
		{
			RescheduleWreckWatch(watch, currentTick, actorPosition, wreckage.Position, unit.State);
			return false;
		}

		_watches.Remove(watch.ActorId);
		reached = new ContactReached(watch.ActorId, new WreckContactTarget(contractId));
		return true;
	}

	private void RescheduleFleetWatch(ContactWatch watch, int currentTick, string targetId)
	{
		var initiator = Map.FleetRegistry.FleetOf(watch.ActorId);
		var target = Map.FleetRegistry.FleetOf(targetId);
		var initiatorPosition = CommittedPositionOf(watch.ActorId);
		var targetPosition = CommittedPositionOf(targetId);
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

	private void RescheduleWreckWatch(
		ContactWatch watch,
		int currentTick,
		Coord actorPosition,
		Coord wreckPosition,
		State actorState)
	{
		var dx = actorPosition.X - wreckPosition.X;
		var dz = actorPosition.Z - wreckPosition.Z;
		var distance = System.Math.Sqrt(dx * dx + dz * dz);
		var gap = System.Math.Max(0, distance - actorState.EngageRadius);
		var maxClosingSpeed = actorState.SpeedPerTick * PathfindingCell.RouteSpeedCeiling;
		var delay = maxClosingSpeed <= 0
			? MaxCheckBackoffTicks
			: (int)System.Math.Clamp(System.Math.Floor(gap / maxClosingSpeed), 1, MaxCheckBackoffTicks);
		watch.NextCheckTick = currentTick + delay;
	}
}
