using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Contact;

internal sealed class ContactMonitor
{
	private const int MaxCheckBackoffTicks = 64;

	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly IPathfinder _pathfinder;
	private readonly Dictionary<string, ContactCheck> _checks = [];
	private bool _promptActive;

	public ContactMonitor(Engine<StarMap, ActorRuntime> engine, IPathfinder pathfinder)
	{
		_engine = engine;
		_pathfinder = pathfinder;
	}

	public event Action? ContactDetected;

	private StarMap Map => _engine.World;

	public Coord CommittedPositionOf(string unitId, float tickFraction = 0f)
	{
		var unit = Map.UnitRegistry.UnitOf(unitId);
		var runtime = _engine.ActorRuntimes.For(unitId);
		TransitCache.RebuildIfMissing(unit, runtime, _pathfinder);
		return unit.State.CommittedPosition(Map, runtime.CachedPath, tickFraction).Position;
	}

	public bool AreInContact(string firstUnitId, string secondUnitId)
	{
		var first = Map.UnitRegistry.UnitOf(firstUnitId).State;
		var second = Map.UnitRegistry.UnitOf(secondUnitId).State;
		var firstPosition = CommittedPositionOf(firstUnitId);
		var secondPosition = CommittedPositionOf(secondUnitId);
		var dx = firstPosition.X - secondPosition.X;
		var dz = firstPosition.Z - secondPosition.Z;
		var distanceSquared = (long)dx * dx + (long)dz * dz;
		var combinedRadius = first.ContactRadius + second.ContactRadius;
		return distanceSquared <= (long)combinedRadius * combinedRadius;
	}

	public void ReconstructFrom(StarMap map)
	{
		foreach (var unit in map.UnitRegistry.All)
		{
			if (unit.State.EngagementTargetUnitId is { } targetId)
				RegisterCheck(unit.State.Id, targetId);
		}
	}

	public void OnHuntCommitted(string initiatorId, string targetId) =>
		RegisterCheck(initiatorId, targetId, immediate: true);

	public void OnHuntCleared(string initiatorId) => _checks.Remove(initiatorId);

	public void OnExitInteractive() => _promptActive = false;

	public void AdvanceTick(int currentTick, bool simModeIsInteractive)
	{
		RunChecks(currentTick, simModeIsInteractive);
	}

	private sealed class ContactCheck
	{
		public required string InitiatorId { get; init; }
		public required string TargetId { get; init; }
		public int NextCheckTick { get; set; }
	}

	private void RegisterCheck(string initiatorId, string targetId, bool immediate = false)
	{
		_checks[initiatorId] = new ContactCheck
		{
			InitiatorId = initiatorId,
			TargetId = targetId,
			NextCheckTick = immediate ? _engine.Tick : _engine.Tick + 1,
		};

		if (immediate)
			RunChecks(_engine.Tick, simModeIsInteractive: false, immediateOnly: true);
	}

	private void RunChecks(int currentTick, bool simModeIsInteractive, bool immediateOnly = false)
	{
		foreach (var check in _checks.Values.ToList())
		{
			if (!immediateOnly && check.NextCheckTick > currentTick)
				continue;

			ProcessCheck(check, currentTick, simModeIsInteractive);
		}
	}

	private void ProcessCheck(ContactCheck check, int currentTick, bool simModeIsInteractive)
	{
		if (!Map.UnitRegistry.TryGet(check.InitiatorId, out var initiator))
		{
			_checks.Remove(check.InitiatorId);
			return;
		}

		var state = initiator.State;
		if (state.EngagementTargetUnitId is not { } targetId
			|| targetId != check.TargetId
			|| state.EngagedWithUnitIds.Count > 0)
		{
			_checks.Remove(check.InitiatorId);
			return;
		}

		if (!Map.UnitRegistry.TryGet(targetId, out _))
		{
			_engine.Commit(new ClearEngagementIntentAction(check.InitiatorId, check.InitiatorId));
			_checks.Remove(check.InitiatorId);
			return;
		}

		if (!AreInContact(check.InitiatorId, targetId))
		{
			RescheduleCheck(check, currentTick);
			return;
		}

		if (_promptActive || simModeIsInteractive)
			return;

		_promptActive = true;
		ContactDetected?.Invoke();
	}

	private void RescheduleCheck(ContactCheck check, int currentTick)
	{
		var initiator = Map.UnitRegistry.UnitOf(check.InitiatorId);
		var target = Map.UnitRegistry.UnitOf(check.TargetId);
		var initiatorPosition = CommittedPositionOf(check.InitiatorId);
		var targetPosition = CommittedPositionOf(check.TargetId);
		var dx = initiatorPosition.X - targetPosition.X;
		var dz = initiatorPosition.Z - targetPosition.Z;
		var distance = System.Math.Sqrt(dx * dx + dz * dz);
		var combinedRadius = initiator.State.ContactRadius + target.State.ContactRadius;
		var gap = System.Math.Max(0, distance - combinedRadius);
		var maxClosingSpeed = (initiator.State.SpeedPerTick + target.State.SpeedPerTick)
			* PathfindingCell.RouteSpeedCeiling;
		var delay = maxClosingSpeed <= 0
			? MaxCheckBackoffTicks
			: (int)System.Math.Clamp(System.Math.Floor(gap / maxClosingSpeed), 1, MaxCheckBackoffTicks);
		check.NextCheckTick = currentTick + delay;
	}
}
