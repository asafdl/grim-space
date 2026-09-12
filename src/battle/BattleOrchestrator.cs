using System.Diagnostics;
using GrimSpace.Core.Log;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Debug;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.Battle.Encounter;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle;

public sealed class BattleOrchestrator : IDisposable
{
	private readonly Engine<BattleWorld, ActorRuntime> _engine;
	private readonly Manager _objectives;
	private readonly ActionBatchSink _actionSink = new();

	private bool _resolveInProgress;
	private int _resolveVersion;

	internal BattleOrchestrator(
		Engine<BattleWorld, ActorRuntime> engine,
		BattleLayout layout,
		string playerId,
		EObjective objective)
	{
		_engine = engine;
		Layout = layout;
		PlayerId = playerId;
		_objectives = new Manager(objective);
	}

	internal Engine<BattleWorld, ActorRuntime> Engine => _engine;

	public BattleLayout Layout { get; }
	public string PlayerId { get; }
	public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;
	public bool IsBattleOver => Outcome.IsOver;
	public int TurnNumber => _engine.Tick;
	public EBattlePhase Phase { get; private set; } = (EBattlePhase)(-1);

	public bool AcceptsPlayerInput => Phase == EBattlePhase.PlayerTurn;

	public event Action<EBattlePhase>? PhaseChanged;
	public event Action<TurnReplay, int>? TurnResolved;

	public UserExecutionAgent PlayerAgent =>
		(UserExecutionAgent)UnitRegistry.For(_engine.World).UnitOf(PlayerId).ExecutionAgent;

	public static BattleOrchestrator FromEncounter(BattleEncounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var timeline = new Timeline();
		var nonUnits = new Dictionary<string, NonUnit>();
		foreach (var spawn in encounter.WorldHazards)
		{
			var hazard = Hazard.Asteroid(
				TypedIdGenerator.NextId("asteroid"),
				spawn.Origin,
				grid,
				spawn.Cells);
			nonUnits[hazard.Id] = hazard;
		}

		var terrainHazards = nonUnits.Values.OfType<Hazard>().ToList();
		var blockedCells = BattleWorld.TerrainBlockedCells(terrainHazards);

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(
				spawn.Unit,
				spawn.Position,
				spawn.ExecutionAgent,
				spawn.InitialMomentum,
				spawn.Fore,
				spawn.Dorsal))
			.ToArray();

		var player = units.First(unit => unit.Alliance.Team == ETeam.Player);
		var world = BattleWorld.FromLive(units, nonUnits, grid, blockedCells, timeline);
		var layout = BattleLayout.FromEncounter(grid, terrainHazards, units);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		foreach (var unit in units) 
			actorRuntimes.For(unit.State.Id);
			
		
		actorRuntimes.For(BattleActorIds.Rules);

		var engine = new Engine<BattleWorld, ActorRuntime>(world, actorRuntimes);
		var orchestrator = new BattleOrchestrator(
			engine,
			layout,
			player.State.Id,
			encounter.Objective);

		foreach (var unit in units)
		{
			ExecutionAgent<BattleWorld, ActorRuntime>.Initialize(
				unit.ExecutionAgent,
				unit.State.Id,
				orchestrator.Engine.CreateSimulation,
				orchestrator._actionSink.WriterFor(unit.State.Id));
		}

		orchestrator.EnterPlayerTurn("encounter ready");
		return orchestrator;
	}

	internal void EnterPlayerTurn(string reason = "player turn") =>
		SetPhase(EBattlePhase.PlayerTurn, reason);

	internal void GrantPlayerCanWork() => SetAgentCanWork(PlayerId, true);

	internal void RevokePlayerCanWork() => SetAgentCanWork(PlayerId, false);

	public Task<ActionProductionResult> WaitForBatchAsync(
		string actorId,
		CancellationToken cancellationToken = default) =>
		_actionSink.WaitForBatchAsync(actorId, cancellationToken);

	public IActionBatchWriter WriterFor(string actorId) => _actionSink.WriterFor(actorId);

	public void EndTurn()
	{
		if (Phase != EBattlePhase.PlayerTurn)
		{
			BattleDiagnostics.LogEndTurnIgnored(Phase);
			return;
		}

		var completedTurn = TurnNumber;
		if (!PlayerAgent.Commit())
		{
			BattleDiagnostics.LogCommitFailed(
				IsBattleOver,
				PlayerAgent.Sim.InvariantStatus,
				TurnNumber,
				PlayerAgent.Sim.Actions.Count);
			return;
		}

		SetPhase(EBattlePhase.Resolving, $"turn {completedTurn} committing");
		var version = ++_resolveVersion;
		_ = ResolveAndReplay(completedTurn, version);
	}

	public void NotifyReplayComplete()
	{
		if (Phase != EBattlePhase.Replaying)
		{
			BattleDiagnostics.LogReplayNotifyIgnored(Phase);
			return;
		}

		if (IsBattleOver)
		{
			SetPhase(EBattlePhase.BattleOver, "battle over after replay");
			return;
		}

		SetPhase(EBattlePhase.PlayerTurn, "replay complete");
	}

	public void Retire()
	{
		if (Phase is EBattlePhase.BattleOver)
			return;

		_resolveVersion++;
		Outcome = BattleOutcome.Lose;
		SetPhase(EBattlePhase.BattleOver, "retired");
	}

	public TurnReplay ResolveTurn() =>
		ResolveTurnAsync().GetAwaiter().GetResult();

	public async Task<TurnReplay> ResolveTurnAsync()
	{
		if (IsBattleOver || _resolveInProgress)
			throw new InvalidOperationException("Cannot resolve turn while battle is over or already resolving.");

		_resolveInProgress = true;
		try
		{
			var replay = await ExecuteTurnAsync();
			Outcome = _objectives.Evaluate(_engine.World, PlayerId);
			return replay;
		}
		finally
		{
			_resolveInProgress = false;
		}
	}

	private async Task<TurnReplay> ExecuteTurnAsync()
	{
		var resolveTimer = Stopwatch.StartNew();
		var turnNumber = TurnNumber;
		var unitsAtTurnStart = SnapshotAll();
		IReadOnlyDictionary<string, UnitState>? unitsAfterPlayer = null;

		_engine.ActorRuntimes.Reset();
		RevokeAllCanWork();

		var units = UnitRegistry.For(_engine.World);
		var activationOrder = units.ActivationOrder.ToList();
		var scheduled = activationOrder.ToHashSet(StringComparer.Ordinal);
		for (var index = 0; index < activationOrder.Count; index++)
		{
			var actorId = activationOrder[index];
			if (!units.TryGet(actorId, out var live) || !live.State.IsAlive)
				continue;

			EnsureAgentInitialized(actorId);
			var batch = await TakeActorBatchAsync(actorId);
			CommitActor(actorId, batch.Actions);
			NotifyWorldUpdated();

			if (actorId == PlayerId)
				unitsAfterPlayer = SnapshotAll();

			var spawnedActors = units.ActivationOrder
				.Where(scheduled.Add)
				.ToList();
			activationOrder.InsertRange(index + 1, spawnedActors);
		}

		CommitRoundUpkeep();
		var history = _engine.History();
		_engine.AdvanceTick();

		GameLog.Log(
			$"Turn {turnNumber} sim: "
			+ $"total={resolveTimer.Elapsed.TotalMilliseconds:F1}ms "
			+ $"history={history.Count}");

		var endStates = SnapshotAll();
		StateLog.LogTurnResolution(
			turnNumber,
			history,
			unitsAtTurnStart,
			unitsAfterPlayer ?? endStates,
			endStates,
			id => ActionLog.DisplayName(units, id));

		return new TurnReplay(unitsAtTurnStart, history, endStates);
	}

	private async Task<ActionBatch> TakeActorBatchAsync(string actorId)
	{
		if (_actionSink.TryTakeBatch(actorId, out var batch))
			return batch;

		SetAgentCanWork(actorId, true);
		try
		{
			if (_actionSink.TryTakeBatch(actorId, out batch))
				return batch;

			var result = await _actionSink.WaitForBatchAsync(actorId);
			if (!result.IsSuccess)
				throw result.Failure!;

			return result.Batch!;
		}
		finally
		{
			SetAgentCanWork(actorId, false);
		}
	}

	private IReadOnlyList<ITimelineEntry> CommitActor(string actorId, IReadOnlyList<IAction> actions)
	{
		var batch = new List<IAction>(actions.Count + 1);
		batch.AddRange(actions);
		batch.Add(new EndOfPhaseAction(actorId));
		return _engine.Commit([..batch]);
	}

	private IReadOnlyList<ITimelineEntry> CommitRoundUpkeep()
	{
		var batch = new List<IAction>();
		foreach (var unitId in UnitRegistry.For(_engine.World).Ids)
			batch.Add(new RoundUpkeepAction(unitId));
		return _engine.Commit([..batch]);
	}

	private Dictionary<string, UnitState> SnapshotAll() =>
		UnitRegistry.For(_engine.World).All.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());

	private void SetPhase(EBattlePhase phase, string reason)
	{
		if (Phase == phase)
			return;

		var from = Phase;
		if (from == EBattlePhase.PlayerTurn)
			RevokePlayerCanWork();

		Phase = phase;
		BattleDiagnostics.LogPhaseTransition(from, phase, reason);
		PhaseChanged?.Invoke(phase);

		if (phase == EBattlePhase.PlayerTurn)
		{
			NotifyWorldUpdated();
			GrantPlayerCanWork();
		}
	}

	private void EnsureAgentInitialized(string actorId)
	{
		var agent = UnitRegistry.For(_engine.World).UnitOf(actorId).ExecutionAgent;
		if (agent.IsInitialized)
			return;

		ExecutionAgent<BattleWorld, ActorRuntime>.Initialize(
			agent,
			actorId,
			_engine.CreateSimulation,
			_actionSink.WriterFor(actorId));
	}

	private void RevokeAllCanWork()
	{
		foreach (var unit in UnitRegistry.For(_engine.World).All)
			unit.ExecutionAgent.SetCanWork(false);
	}

	private void SetAgentCanWork(string actorId, bool canWork) =>
		UnitRegistry.For(_engine.World).UnitOf(actorId).ExecutionAgent.SetCanWork(canWork);

	private void NotifyWorldUpdated()
	{
		foreach (var unit in UnitRegistry.For(_engine.World).All)
			unit.ExecutionAgent.OnWorldUpdated();
	}

	private async Task ResolveAndReplay(int completedTurn, int version)
	{
		var resolveTimer = Stopwatch.StartNew();
		try
		{
			var replay = await ResolveTurnAsync();
			resolveTimer.Stop();

			if (version != _resolveVersion)
			{
				BattleDiagnostics.LogResolveAborted($"resolve_job_stale v{version}", Phase);
				return;
			}

			if (Phase != EBattlePhase.Resolving)
			{
				BattleDiagnostics.LogResolveAborted("unexpected_phase", Phase);
				return;
			}

			TurnPresentationTiming.LogResolveWait(completedTurn, resolveTimer.Elapsed.TotalMilliseconds);
			SetPhase(EBattlePhase.Replaying, $"turn {completedTurn} resolved");
			TurnResolved?.Invoke(replay, completedTurn);
		}
		catch (Exception ex) when (version == _resolveVersion && Phase == EBattlePhase.Resolving)
		{
			BattleDiagnostics.LogJobFailed(ex);
			throw new InvalidOperationException("Turn resolve failed after commit.", ex);
		}
	}

	public void Dispose() => _engine.Dispose();
}
