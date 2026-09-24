using GrimSpace.Battle.Objectives;
using GrimSpace.Run;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Agents;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Presentation.Diagnostics;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Narrative;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Units;
using BattleUnitType = GrimSpace.Units.Enums.EType;

namespace GrimSpace.World.StarSystem;

public sealed class StarSystemOrchestrator : IDisposable
{
	private readonly Engine<StarMap, ActorRuntime> _engine;
	private readonly ContactMonitor _contactMonitor;
	private readonly ActionBatchSink _actionSink = new();
	private readonly StarMapPlayerExecutionAgent? _playerAgent;
	private readonly IReadOnlyList<(TrafficExecutionAgent Agent, string ActorId)> _trafficAgents;
	private readonly ContractBoardExecutionAgent _contractBoardAgent;
	private readonly Queue<IAction> _reactionQueue = [];
	private bool _contractGenerationEnabled = true;
	private readonly IDisposable _storyObjectiveSubscription;
	private readonly IDisposable _engagementResolvedSubscription;
	private readonly IDisposable _deliveryTurnInSubscription;
	private readonly IDisposable _wreckageInvestigationSubscription;
	private readonly IDisposable _resourceTransactionSubscription;
	private ESimMode _simMode = (ESimMode)(-1);
	private bool _resolvingInputAction;

	private StarSystemOrchestrator(
		Engine<StarMap, ActorRuntime> engine,
		ContactMonitor contactMonitor,
		string? playerId,
		StarMapPlayerExecutionAgent? playerAgent,
		IReadOnlyList<(TrafficExecutionAgent Agent, string ActorId)> trafficAgents,
		ContractBoardExecutionAgent contractBoardAgent)
	{
		_engine = engine;
		_contactMonitor = contactMonitor;
		PlayerId = playerId;
		_playerAgent = playerAgent;
		_trafficAgents = trafficAgents;
		_contractBoardAgent = contractBoardAgent;
		_storyObjectiveSubscription = _engine.Subscribe<AcceptContractAction>(OnContractAccepted);
		_engagementResolvedSubscription = _engine.Subscribe<ResolveEngagementAction>(OnEngagementResolved);
		_deliveryTurnInSubscription = _engine.Subscribe<TurnInDeliveryAction>(OnDeliveryTurnedIn);
		_wreckageInvestigationSubscription = _engine.Subscribe<InvestigateWreckageAction>(OnWreckageInvestigated);
		_resourceTransactionSubscription =
			_engine.Subscribe<Record<Transaction>>(record => ResourceTransactionCommitted?.Invoke(record.Value));
	}

	public event Action? WorldUpdated;
	public event Action<Transaction>? ResourceTransactionCommitted;

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

	public bool ContractGenerationEnabled => _contractGenerationEnabled;

	public void SetContractGenerationEnabled(bool enabled) => _contractGenerationEnabled = enabled;

	public ActorRuntime RuntimeFor(string unitId) => _engine.ActorRuntimes.For(unitId);

	public Coord CommittedPositionOf(string unitId, float tickFraction = 0f) =>
		_contactMonitor.CommittedPositionOf(unitId, tickFraction);

	public IDisposable Subscribe<TEntry>(Action<TEntry> listener)
		where TEntry : ITimelineEntry =>
		_engine.Subscribe(listener);

	public Simulation<StarMap, ActorRuntime> CreateSimulation() => _engine.CreateSimulation();

	public static StarSystemOrchestrator CreateSession(string playerFleetUnitId, int seed = 0)
	{
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		var defaultShipId = Core.Ids.TypedIdGenerator.NextId(UnitTypeSlug.For(BattleUnitType.Fighter));
		return CreateSession(playerFleetUnitId, [defaultShipId], seed);
	}

	public static StarSystemOrchestrator CreateSession(
		string playerFleetUnitId,
		IReadOnlyList<string> playerShipIds,
		int seed = 0)
	{
		ArgumentException.ThrowIfNullOrEmpty(playerFleetUnitId);
		ArgumentNullException.ThrowIfNull(playerShipIds);
		if (playerShipIds.Count == 0)
			throw new ArgumentException("Player fleet must contain at least one ship.", nameof(playerShipIds));
		var map = StarMap.Create(seed);
		AddPlayerFleet(map, playerFleetUnitId, playerShipIds);
		return InitializeSession(map, playerFleetUnitId);
	}

	private static StarSystemOrchestrator InitializeSession(StarMap map, string playerFleetUnitId)
	{
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

		foreach (var unit in map.FleetRegistry.All)
		{
			var runtime = actorRuntimes.For(unit.State.Id);
			TransitCache.RebuildIfMissing(unit, runtime, pathfinder);
			ScheduleSpawnedWorkerIfNeeded(map, unit);
		}

		actorRuntimes.For(StarSystemActorIds.Contracts);

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

		var trafficUnits = map.FleetRegistry.All
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

		StarSystemOrchestrator orchestrator = null!;
		var contractBoardAgent = new ContractBoardExecutionAgent(
			() => engine.World,
			id => engine.ActorRuntimes.For(id),
			() => orchestrator.ContractGenerationEnabled);
		orchestrator = new StarSystemOrchestrator(
			engine,
			contactMonitor,
			playerId,
			playerAgent,
			trafficAgents,
			contractBoardAgent);

		contractBoardAgent.Init(
			StarSystemActorIds.Contracts,
			orchestrator._actionSink.WriterFor(StarSystemActorIds.Contracts));

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

	private static void AddPlayerFleet(
		StarMap map,
		string playerFleetUnitId,
		IReadOnlyList<string> shipIds)
	{
		var members = shipIds.Select(id => new FleetMember(id)).ToArray();
		map.FleetRegistry.Add(new Fleet(
			Units.State.FromSpawn(CreatePlayerFleetSpawn(map, playerFleetUnitId)),
			members));
	}

	//TODO: we should not be initializing this via orchestrator, this is bad design
	private static Spawn CreatePlayerFleetSpawn(StarMap map, string playerFleetUnitId)
	{
		var tradeHubDock = map.DocksByPoiId[SupplySystemPlan.Copper.TradeHubPoiId];
		return new Spawn(
			playerFleetUnitId,
			EType.PlayerFleet,
			tradeHubDock.Id,
			default,
			UnitDefaults.SpeedPerTick(EType.PlayerFleet),
			UnitDefaults.EngageRadius(EType.PlayerFleet),
			UnitDefaults.VisionRadius(EType.PlayerFleet),
			[],
			Factions.EFaction.Player);
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
		CommitReactions();
		CommitContractBoardActions();
		NotifyWorldUpdated();
		return history;
	}

	public void CommitSetup(params IAction<StarMap, ActorRuntime>[] actions)
	{
		if (actions.Length == 0)
			return;

		Commit(actions);
		NotifyWorldUpdated();
	}

	public bool ResolveEngagement(string playerId, BattleOutcome outcome)
	{
		var loot = LootCatalog.For(outcome);
		var action = new ResolveEngagementAction(
			playerId,
			outcome,
			loot.Rolls,
			loot.Total);
		if (!ResolveEngagementDef.Instance.IsLegal(action, Map, _engine.ActorRuntimes.For(action)))
			return false;

		Commit(action);
		NotifyWorldUpdated();
		return true;
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
		CommitReactions();

		CommitContactActions();
		CommitContractBoardActions();
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

		_contractBoardAgent.SetCanWork(trafficCanWork);
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

		Commit([..batch.Actions]);
	}

	private void CommitTrafficActions()
	{
		foreach (var (agent, actorId) in _trafficAgents)
		{
			agent.PlanAndPublish();
			if (!_actionSink.TryTakeBatch(actorId, out var batch) || batch.Actions.Count == 0)
				continue;

			Commit([..batch.Actions]);
		}
	}

	private void CommitContactActions()
	{
		var produced = _contactMonitor.Update(Tick);
		if (produced.Count > 0)
			Commit([..produced]);
	}

	private void CommitContractBoardActions()
	{
		_contractBoardAgent.PlanAndPublish();
		if (!_actionSink.TryTakeBatch(StarSystemActorIds.Contracts, out var batch) || batch.Actions.Count == 0)
			return;

		_engine.Commit([..batch.Actions]);
	}

	private void OnContractAccepted(AcceptContractAction accepted)
	{
		foreach (var reaction in StoryObjectiveFulfillment.ReactionsFor(Map, PlayerId, accepted))
		{
			if (!_reactionQueue.Contains(reaction))
				_reactionQueue.Enqueue(reaction);
		}
	}

	private void OnEngagementResolved(ResolveEngagementAction resolved)
	{
		EnqueueContractCompletions(resolved.InitiatorId, EContractKind.Hunt);
	}

	private void OnDeliveryTurnedIn(TurnInDeliveryAction turnedIn)
	{
		EnqueueContractCompletions(turnedIn.ActorId, EContractKind.Delivery);
	}

	private void OnWreckageInvestigated(InvestigateWreckageAction investigated)
	{
		EnqueueContractCompletions(investigated.ActorId, EContractKind.Wreckage);
	}

	private void EnqueueContractCompletions(string actorId, EContractKind? kind = null)
	{
		foreach (var reaction in ContractReevaluation.ReevaluateFor(
			Map,
			_engine.ActorRuntimes.For(actorId),
			actorId,
			kind))
		{
			if (!_reactionQueue.Contains(reaction))
				_reactionQueue.Enqueue(reaction);
		}
	}

	private IReadOnlyList<ITimelineEntry> Commit(params IAction[] actions)
	{
		var history = _engine.Commit(actions);
		CommitReactions();
		return history;
	}

	private void CommitReactions()
	{
		while (_reactionQueue.Count > 0)
			_engine.Commit(_reactionQueue.Dequeue());
	}

	private void NotifyWorldUpdated()
	{
		WorldUpdated?.Invoke();
		_playerAgent?.OnWorldUpdated();
	}

	public void Dispose()
	{
		_storyObjectiveSubscription.Dispose();
		_engagementResolvedSubscription.Dispose();
		_deliveryTurnInSubscription.Dispose();
		_wreckageInvestigationSubscription.Dispose();
		_resourceTransactionSubscription.Dispose();
		_engine.Dispose();
	}

	private static void ScheduleSpawnedWorkerIfNeeded(StarMap map, Units.Fleet unit)
	{
		var state = unit.State;
		if (state.Phase != EPhase.Working || state.SpawnWorkPoiId is not { } poiId)
			return;

		WorkScheduler.ScheduleSpawnedWorker(map, unit, poiId, state.SpawnWorkRemainingTicks);
		state.SpawnWorkPoiId = null;
		state.SpawnWorkRemainingTicks = 0;
	}
}
