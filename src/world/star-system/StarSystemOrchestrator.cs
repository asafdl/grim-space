using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Presentation;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem;

public sealed class StarSystemOrchestrator : IDisposable
{
	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly ContactMonitor _contactMonitor;
	private readonly ActionBatchSink _actionSink = new();
	private readonly StarMapPlayerExecutionAgent? _playerAgent;
	private readonly IReadOnlyList<(TrafficExecutionAgent Agent, string ActorId)> _trafficAgents;
	private ESimMode _simMode = (ESimMode)(-1);
	private bool _resolvingInputAction;

	private StarSystemOrchestrator(
		Engine<StarMap, ActorRuntime> engine,
		ContactMonitor contactMonitor,
		string? playerId,
		StarMapPlayerExecutionAgent? playerAgent,
		IReadOnlyList<(TrafficExecutionAgent Agent, string ActorId)> trafficAgents)
	{
		_engine = engine;
		_contactMonitor = contactMonitor;
		PlayerId = playerId;
		_playerAgent = playerAgent;
		_trafficAgents = trafficAgents;
	}

	public event Action? WorldUpdated;

	public StarMap Map => _engine.World;

	public int Tick => _engine.Tick;

	public string? PlayerId { get; }

	public StarMapPlayerExecutionAgent? PlayerAgent => _playerAgent;

	public ESimMode SimMode => _simMode;

	public bool IsRunning => _simMode == ESimMode.Running;

	public bool IsStepped => _simMode == ESimMode.Stepped;

	public bool CanAdvance =>
		_simMode == ESimMode.Running
		&& !Map.WaitingForPlayerInput;

	public ActorRuntime RuntimeFor(string unitId) => _engine.ActorRuntimes.For(unitId);

	public Coord CommittedPositionOf(string unitId, float tickFraction = 0f) =>
		_contactMonitor.CommittedPositionOf(unitId, tickFraction);

	public Simulation<StarMap, ActorRuntime> CreateSimulation() => _engine.CreateSimulation();

	public static StarSystemOrchestrator CreateDevSession(string playerFleetUnitId, int seed = 0)
	{
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var map = StarMap.CreateDevDefault(seed);
		AddPlayerFleet(map, playerFleetUnitId);
		var orchestrator = FromMap(map, playerFleetUnitId);
		orchestrator.CommitSetup(
			new BeginNarrativeAction(playerFleetUnitId, MapNarratives.OpeningId));
		return orchestrator;
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
			.Select(unit => (
				new TrafficExecutionAgent(
					() => engine.World,
					unitId => engine.ActorRuntimes.For(unitId),
					pathfinder),
				unit.State.Id))
			.ToArray();

		var orchestrator = new StarSystemOrchestrator(
			engine,
			contactMonitor,
			playerId,
			playerAgent,
			trafficAgents);

		if (playerAgent is not null)
		{
			playerAgent.Init(
				playerId!,
				engine.CreateSimulation,
				orchestrator._actionSink.WriterFor(playerId!));
			playerAgent.PlanningChanged += orchestrator.OnPlayerPlanningChanged;
		}

		foreach (var (agent, actorId) in trafficAgents)
			agent.Init(actorId, orchestrator._actionSink.WriterFor(actorId));

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
			UnitDefaults.EngageRadius(EType.PlayerFleet),
			[])));
	}

	public void SetRunning() => ApplySimMode(ESimMode.Running);

	public void SetStepped() => ApplySimMode(ESimMode.Stepped);

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
		NotifyWorldUpdated();
		return history;
	}

	public void CommitSetup(params IAction<StarMap, ActorRuntime>[] actions)
	{
		if (actions.Length == 0)
			return;

		_engine.Commit(actions);
		NotifyWorldUpdated();
	}

	public void ResolveEngagement(string playerId, BattleOutcome outcome)
	{
		_engine.Commit([new ResolveEngagementAction(playerId, outcome)]);
		NotifyWorldUpdated();
	}

	public void RefreshPlayerAgent() => _playerAgent?.OnWorldUpdated();

	public bool TryCommitPlayerInput(IAction action)
	{
		if (_playerAgent is null || PlayerId is null)
		{
			StarMapPresentationDiagnostics.LogInputRejected(action, "no_player_agent");
			return false;
		}

		RefreshPlayerAgent();

		if (!_playerAgent.TryEnqueue([action]))
			return false;

		var advancedClock = !Map.WaitingForPlayerInput;
		if (advancedClock)
			AdvanceClock();

		StarMapPresentationDiagnostics.LogInputCommitted(action, advancedClock);
		return true;
	}

	public IReadOnlyList<ITimelineEntry> AdvanceTick()
	{
		CommitPlayerActions();
		CommitTrafficActions();

		var history = _engine.AdvanceTick();

		if (PlayerId is not null)
			ContractFulfillment.Evaluate(_engine.World, PlayerId);

		CommitContactActions();
		NotifyWorldUpdated();
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

		_simMode = mode;
		ApplyCanWorkPolicy();
		NotifyWorldUpdated();
	}

	private void ApplyCanWorkPolicy()
	{
		var trafficCanWork = _simMode is ESimMode.Running or ESimMode.Stepped;
		foreach (var (agent, _) in _trafficAgents)
			agent.SetCanWork(trafficCanWork);

		_playerAgent?.SetCanWork(PlayerId is not null);
	}

	private void OnPlayerPlanningChanged()
	{
		if (_playerAgent?.HasPendingAction != true || _resolvingInputAction)
			return;

		if (PlayerId is null || !Map.WaitingForPlayerInput)
			return;

		_resolvingInputAction = true;
		try
		{
			AdvanceClock();
		}
		finally
		{
			_resolvingInputAction = false;
		}
	}

	private void CommitPlayerActions()
	{
		if (_playerAgent is null || PlayerId is null)
			return;

		if (!_playerAgent.Commit())
			return;

		if (!_actionSink.TryTakeBatch(PlayerId, out var batch) || batch.Actions.Count == 0)
		{
			StarMapPresentationDiagnostics.LogCommitSkipped("empty_batch_after_commit", _playerAgent);
			RefreshPlayerAgent();
			return;
		}

		_engine.Commit([..batch.Actions]);
	}

	private void CommitTrafficActions()
	{
		foreach (var (agent, actorId) in _trafficAgents)
		{
			agent.PlanAndPublish();
			if (!_actionSink.TryTakeBatch(actorId, out var batch) || batch.Actions.Count == 0)
				continue;

			_engine.Commit([..batch.Actions]);
		}
	}

	private void CommitContactActions()
	{
		var produced = _contactMonitor.Update(Tick);
		if (produced.Count > 0)
			_engine.Commit([..produced]);
	}

	private void NotifyWorldUpdated()
	{
		WorldUpdated?.Invoke();
		_playerAgent?.OnWorldUpdated();
	}

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
