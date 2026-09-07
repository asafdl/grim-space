using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed class StarSystemOrchestrator : IDisposable
{
	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly ContactMonitor _contactMonitor;
	private readonly StarMapPlayerExecutionAgent? _playerAgent;
	private readonly IReadOnlyList<TrafficExecutionAgent> _trafficAgents;
	private ESimMode _simMode = ESimMode.Running;
	private ESimMode _modeBeforeInteractive;
	private bool _resolvingInteractiveAction;
	private event Action<string?>? ActiveUnitChanged;

	private StarSystemOrchestrator(
		Engine<StarMap, ActorRuntime> engine,
		ContactMonitor contactMonitor,
		string? playerId,
		StarMapPlayerExecutionAgent? playerAgent,
		IReadOnlyList<TrafficExecutionAgent> trafficAgents)
	{
		_engine = engine;
		_contactMonitor = contactMonitor;
		PlayerId = playerId;
		_playerAgent = playerAgent;
		_trafficAgents = trafficAgents;
		_contactMonitor.ContactDetected += OnContactDetected;
	}

	public StarMap Map => _engine.World;

	public int Tick => _engine.Tick;

	public string? PlayerId { get; }

	public StarMapPlayerExecutionAgent? PlayerAgent => _playerAgent;

	public ESimMode SimMode => _simMode;

	public bool IsRunning => _simMode == ESimMode.Running;

	public bool IsStepped => _simMode == ESimMode.Stepped;

	public ActorRuntime RuntimeFor(string unitId) => _engine.ActorRuntimes.For(unitId);

	public Coord CommittedPositionOf(string unitId, float tickFraction = 0f) =>
		_contactMonitor.CommittedPositionOf(unitId, tickFraction);

	public bool AreInContact(string firstUnitId, string secondUnitId) =>
		_contactMonitor.AreInContact(firstUnitId, secondUnitId);

	public Simulation<StarMap, ActorRuntime> CreateSimulation() => _engine.CreateSimulation();

	public static StarSystemOrchestrator CreateDevSession(string playerFleetUnitId, int seed = 0)
	{
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var map = StarMap.CreateDevDefault(seed);
		AddPlayerFleet(map, playerFleetUnitId);
		return FromMap(map, playerFleetUnitId);
	}

	public static StarSystemOrchestrator FromMap(StarMap map) =>
		FromMap(map, new CachedPathfinder(new GridPathfinder(map.PathfindingTerrain)), playerId: null);

	public static StarSystemOrchestrator FromMap(StarMap map, string playerId) =>
		FromMap(map, new CachedPathfinder(new GridPathfinder(map.PathfindingTerrain)), playerId);

	public static StarSystemOrchestrator FromMap(
		StarMap map,
		IPathfinder pathfinder,
		string? playerId = null)
	{
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		if (map.Timeline.Clock.Current == 0)
			map.Timeline.Clock.Set(1);

		foreach (var unit in map.UnitRegistry.All)
		{
			var runtime = actorRuntimes.For(unit.State.Id);
			TransitCache.RebuildIfMissing(unit, runtime, pathfinder);
			ScheduleSpawnedWorkerIfNeeded(map, unit);
		}

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		var contactMonitor = new ContactMonitor(engine, pathfinder);
		StarMapPlayerExecutionAgent? playerAgent = null;
		if (playerId is not null)
		{
			playerAgent = new StarMapPlayerExecutionAgent(
				engine.CreateSimulation,
				() => engine.World,
				unitId => engine.ActorRuntimes.For(unitId),
				unitId => contactMonitor.CommittedPositionOf(unitId),
				pathfinder);
		}

		var trafficUnits = map.UnitRegistry.All
			.Where(unit => unit.State.ChoreDockIds.Count > 0)
			.OrderBy(unit => unit.State.Id, StringComparer.Ordinal)
			.ToArray();
		var trafficAgents = trafficUnits
			.Select(_ => new TrafficExecutionAgent(pathfinder))
			.ToArray();

		var orchestrator = new StarSystemOrchestrator(
			engine,
			contactMonitor,
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

		contactMonitor.ReconstructFrom(map);
		orchestrator.ApplySimMode(ESimMode.Running);
		return orchestrator;
	}

	private static void AddPlayerFleet(StarMap map, string playerFleetUnitId)
	{
		var tradeHubDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		map.UnitRegistry.Add(Factory.Create(new Spawn(
			playerFleetUnitId,
			EType.PlayerFleet,
			tradeHubDock.Id,
			default,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			UnitDefaults.ContactRadius(EType.PlayerFleet),
			[])));
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

		_contactMonitor.OnExitInteractive();
		ApplySimMode(_modeBeforeInteractive);
	}

	public void DismissEngagement()
	{
		if (PlayerId is { } playerId
			&& Map.StateOf(playerId).EngagementTargetUnitId is not null)
		{
			_engine.Commit(new ClearEngagementIntentAction(playerId, playerId));
			_contactMonitor.OnHuntCleared(playerId);
		}

		ExitInteractive();
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
		if (PlayerId is not null)
			ContractFulfillment.Evaluate(_engine.World, PlayerId);
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

		if (PlayerId is not null)
			ContractFulfillment.Evaluate(_engine.World, PlayerId);

		if (_playerAgent is not null && PlayerId is not null)
			SetActive(PlayerId);

		_contactMonitor.AdvanceTick(Tick, _simMode == ESimMode.Interactive);
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
		if (playerActions.Count == 0)
			return;

		_engine.Commit([..playerActions]);
		foreach (var action in playerActions)
		{
			if (action is HuntUnitAction hunt)
				_contactMonitor.OnHuntCommitted(hunt.ActorId, hunt.TargetUnitId);
		}
	}

	private void OnContactDetected()
	{
		EnterInteractive();
		ResetPlayerAgentPlanning();
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

	public void Dispose() => _engine.Dispose();

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
