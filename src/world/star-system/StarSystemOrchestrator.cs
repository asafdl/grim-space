using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed class StarSystemOrchestrator
{
	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly StarMapPlayerExecutionAgent? _playerAgent;
	private readonly IReadOnlyList<TrafficExecutionAgent> _trafficAgents;
	private ESimMode _simMode = ESimMode.Running;
	private ESimMode _modeBeforeInteractive;
	private bool _resolvingInteractiveAction;
	private event Action<string?>? ActiveUnitChanged;

	private StarSystemOrchestrator(
		Engine<StarMap, ActorRuntime> engine,
		string? playerId,
		StarMapPlayerExecutionAgent? playerAgent,
		IReadOnlyList<TrafficExecutionAgent> trafficAgents)
	{
		_engine = engine;
		PlayerId = playerId;
		_playerAgent = playerAgent;
		_trafficAgents = trafficAgents;
	}

	public StarMap Map => _engine.World;

	public int Tick => _engine.Tick;

	public string? PlayerId { get; }

	public StarMapPlayerExecutionAgent? PlayerAgent => _playerAgent;

	public ESimMode SimMode => _simMode;

	public bool IsRunning => _simMode == ESimMode.Running;

	public bool IsStepped => _simMode == ESimMode.Stepped;

	public static StarSystemOrchestrator FromBuildResult(StarSystemBuildResult result) =>
		FromBuildResult(result, new CachedPathfinder(new AStarPathfinder(result.Terrain)), playerId: null);

	public static StarSystemOrchestrator FromBuildResult(
		StarSystemBuildResult result,
		string playerId) =>
		FromBuildResult(
			result,
			new CachedPathfinder(new AStarPathfinder(result.Terrain)),
			playerId);

	public static StarSystemOrchestrator FromBuildResult(
		StarSystemBuildResult result,
		IPathfinder pathfinder,
		string? playerId = null)
	{
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		if (result.Map.Timeline.Clock.Current == 0)
			result.Map.Timeline.Clock.Set(1);

		foreach (var unit in result.Map.UnitRegistry.All)
		{
			actorRuntimes.Register(unit.State.Id, unit.Runtime);
			TransitCache.RebuildIfMissing(unit, pathfinder);
			ScheduleSpawnedWorkerIfNeeded(result.Map, unit);
		}

		var engine = new Engine<StarMap, ActorRuntime>(result.Map, actorRuntimes);
		StarMapPlayerExecutionAgent? playerAgent = null;
		if (playerId is not null)
		{
			playerAgent = new StarMapPlayerExecutionAgent(
				engine.CreateSimulation,
				() => engine.World,
				pathfinder);
		}

		var trafficUnits = result.Map.UnitRegistry.All
			.Where(unit => unit.State.ChoreDockIds.Count > 0)
			.OrderBy(unit => unit.State.Id, StringComparer.Ordinal)
			.ToArray();
		var trafficAgents = trafficUnits
			.Select(_ => new TrafficExecutionAgent(pathfinder))
			.ToArray();

		var orchestrator = new StarSystemOrchestrator(
			engine,
			playerId,
			playerAgent,
			trafficAgents);

		if (playerAgent is not null)
		{
			playerAgent.Init(playerId!, engine.CreateSimulation, orchestrator.RegisterActiveUnitChanged);
			orchestrator.SetActive(playerId);
		}

		for (var i = 0; i < trafficAgents.Length; i++)
		{
			trafficAgents[i].Init(
				trafficUnits[i].State.Id,
				engine.CreateSimulation,
				orchestrator.RegisterActiveUnitChanged);
		}

		orchestrator.ApplySimMode(ESimMode.Running);
		return orchestrator;
	}

	public void SetRunning() => ApplySimMode(ESimMode.Running);

	public void SetStepped() => ApplySimMode(ESimMode.Stepped);

	public void EnterInteractive()
	{
		if (_simMode == ESimMode.Interactive)
			return;

		_modeBeforeInteractive = _simMode;
		ApplySimMode(ESimMode.Interactive);
	}

	public void ExitInteractive()
	{
		if (_simMode != ESimMode.Interactive)
			return;

		ApplySimMode(_modeBeforeInteractive);
	}

	public void TogglePause()
	{
		if (_simMode == ESimMode.Running)
			SetStepped();
		else if (_simMode == ESimMode.Stepped)
			SetRunning();
	}

	public void Step()
	{
		if (_simMode != ESimMode.Stepped)
			return;

		AdvanceTick();
	}

	public IReadOnlyList<ITimelineEntry> AdvanceClock()
	{
		CommitPlayerActions();
		var history = _engine.AdvanceTick();
		ResetPlayerAgentPlanning();
		return history;
	}

	public IReadOnlyList<ITimelineEntry> AdvanceTick()
	{
		CommitPlayerActions();

		foreach (var agent in _trafficAgents)
		{
			SetActive(agent.ActorId);
			var actions = agent.TakeCompletedActions();
			if (actions.Count > 0)
				_engine.Commit([..actions]);
		}

		var history = _engine.AdvanceTick();

		if (_playerAgent is not null && PlayerId is not null)
			SetActive(PlayerId);

		return history;
	}

	public void AdvanceTicks(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		for (var i = 0; i < count; i++)
			AdvanceTick();
	}

	private void ApplySimMode(ESimMode mode)
	{
		if (_simMode == mode)
			return;

		UnwireInteractivePlanning();
		_simMode = mode;
		WireInteractivePlanning();
	}

	private void WireInteractivePlanning()
	{
		if (_playerAgent is null || _simMode != ESimMode.Interactive)
			return;

		_playerAgent.PlanningChanged += OnInteractivePlanningChanged;
	}

	private void UnwireInteractivePlanning()
	{
		if (_playerAgent is null)
			return;

		_playerAgent.PlanningChanged -= OnInteractivePlanningChanged;
	}

	private void OnInteractivePlanningChanged()
	{
		if (_simMode != ESimMode.Interactive
			|| _playerAgent?.HasPendingAction != true
			|| _resolvingInteractiveAction)
			return;

		_resolvingInteractiveAction = true;
		try
		{
			AdvanceClock();
		}
		finally
		{
			_resolvingInteractiveAction = false;
		}
	}

	private void CommitPlayerActions()
	{
		if (_playerAgent is null)
			return;

		if (!_playerAgent.Commit())
			return;

		var playerActions = _playerAgent.TakeCompletedActions();
		if (playerActions.Count > 0)
			_engine.Commit([..playerActions]);
	}

	private void ResetPlayerAgentPlanning()
	{
		if (_playerAgent is null || PlayerId is null)
			return;

		SetActive(null);
		SetActive(PlayerId);
	}

	private void RegisterActiveUnitChanged(Action<string?> handler) =>
		ActiveUnitChanged += handler;

	private void SetActive(string? unitId) => ActiveUnitChanged?.Invoke(unitId);

	private static void ScheduleSpawnedWorkerIfNeeded(StarMap map, Units.Unit unit)
	{
		var state = unit.State;
		if (state.Phase != EPhase.Working || state.SpawnWorkPoiId is not { } poiId)
			return;

		WorkScheduler.ScheduleSpawnedWorker(map, unit, poiId, state.SpawnWorkRemainingTicks);
		state.SpawnWorkPoiId = null;
		state.SpawnWorkRemainingTicks = 0;
	}
}
