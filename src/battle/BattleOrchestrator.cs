using System.Diagnostics;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Debug;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
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
	private readonly ActionBatchSink _actionSink = new();

	private bool _resolveInProgress;
	private int _resolveVersion;
	private readonly HashSet<string> _activatedThisRound = new(StringComparer.Ordinal);

	internal BattleOrchestrator(
		Engine<BattleWorld, ActorRuntime> engine,
		BattleLayout layout,
		string playerId)
	{
		_engine = engine;
		Layout = layout;
		ActivePlayerId = playerId;
	}

	internal Engine<BattleWorld, ActorRuntime> Engine => _engine;

	public BattleLayout Layout { get; }
	public string ActivePlayerId { get; private set; }
	public string PlayerId => ActivePlayerId;
	public bool IsBattleOver => _engine.World.battleResult != EBattleResult.Ongoing;
	public int TurnNumber => _engine.Tick;
	public EBattlePhase Phase { get; private set; } = (EBattlePhase)(-1);
	public bool IsAtRoundStart => _activatedThisRound.Count == 0;
	public TurnReplay? PendingReplay { get; private set; }
	public int PendingReplayTurn { get; private set; }

	public bool AcceptsPlayerInput => Phase == EBattlePhase.PlayerTurn;
	public bool CanForceOutcome => Phase == EBattlePhase.PlayerTurn && !IsBattleOver;

	public event Action<EBattlePhase>? PhaseChanged;
	public event Action<TurnReplay, int>? TurnResolved;

	public UserExecutionAgent PlayerAgent =>
		(UserExecutionAgent)UnitRegistry.For(_engine.World).UnitOf(ActivePlayerId).ExecutionAgent;

	public static BattleOrchestrator FromEncounter(
		BattleEncounter encounter,
		int gridSize = 64,
		string? preferredPlayerId = null)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var timeline = new Timeline();
		var nonUnits = new Dictionary<string, NonUnit>();
		foreach (var spawn in encounter.WorldHazards)
		{
			var asteroid = Asteroid.Create(
				TypedIdGenerator.NextId(NonUnitTypeSlug.Asteroid),
				spawn.Origin,
				grid,
				spawn.Cells);
			nonUnits[asteroid.Id] = asteroid;
		}

		var asteroids = nonUnits.Values.OfType<Asteroid>().ToList();
		var blockedCells = BattleWorld.TerrainBlockedCells(asteroids);

		var units = encounter.Spawns
			.Select(Factory.Create)
			.ToArray();

		var engagedShipIds = encounter.Spawns
			.Select(spawn => spawn.Ship.Id)
			.ToHashSet(StringComparer.Ordinal);
		var playerUnits = units.Where(unit => unit.Team == ETeam.Player).ToArray();
		var player = playerUnits[0];
		if (preferredPlayerId is not null)
		{
			var preferred = playerUnits.FirstOrDefault(unit =>
				unit.State.Id.Equals(preferredPlayerId, StringComparison.Ordinal));
			if (preferred is not null)
				player = preferred;
		}

		var world = BattleWorld.FromLive(
			units,
			nonUnits,
			grid,
			blockedCells,
			encounter.Id,
			encounter.Objective,
			engagedShipIds,
			timeline);
		var layout = BattleLayout.FromEncounter(grid, asteroids, units);
		return CreateFromWorld(world, layout, player.State.Id, "encounter ready");
	}

	public static BattleOrchestrator FromWorld(BattleWorld world, string playerId)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentException.ThrowIfNullOrEmpty(playerId);

		if (!world.UnitRegistry.TryGet(playerId, out var player) || player.Team != ETeam.Player)
			throw new InvalidOperationException($"Battle world has no player unit '{playerId}'.");

		var layout = new BattleLayout(
			world.Grid,
			world.Asteroids.ToList(),
			world.UnitRegistry.All.ToDictionary(unit => unit.State.Id, unit => unit.Team));
		return CreateFromWorld(world, layout, playerId, "battle world restored");
	}

	public static BattleOrchestrator FromSavedWorld(BattleWorld world, string playerId)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentException.ThrowIfNullOrEmpty(playerId);

		var savedUnits = world.UnitRegistry.All.ToArray();
		foreach (var savedUnit in savedUnits)
		{
			var agentKind = BattleAgentFactory.KindOf(savedUnit.ExecutionAgent);
			world.UnitRegistry.Remove(savedUnit.State.Id);
			world.UnitRegistry.Add(
				Factory.Create(savedUnit.State.Clone(), savedUnit.Team, agentKind));
		}

		return FromWorld(world, playerId);
	}

	private static BattleOrchestrator CreateFromWorld(
		BattleWorld world,
		BattleLayout layout,
		string playerId,
		string phaseReason)
	{
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		foreach (var unit in world.UnitRegistry.All)
			actorRuntimes.For(unit.State.Id);
		actorRuntimes.For(BattleActorIds.Rules);

		var engine = new Engine<BattleWorld, ActorRuntime>(world, actorRuntimes);
		var orchestrator = new BattleOrchestrator(engine, layout, playerId);

		foreach (var unit in world.UnitRegistry.All)
		{
			var writer = orchestrator._actionSink.WriterFor(unit.State.Id);
			if (unit.ExecutionAgent is SimulationExecutionAgent<BattleWorld, ActorRuntime> simulationAgent)
			{
				simulationAgent.Rebind(unit.State.Id, orchestrator.Engine.CreateSimulation, writer);
				simulationAgent.OnWorldUpdated();
			}
			else
				unit.ExecutionAgent.Rebind(unit.State.Id, writer);
		}

		if (world.battleResult == EBattleResult.Ongoing)
			orchestrator.BeginRound(phaseReason);
		else
			orchestrator.SetPhase(EBattlePhase.BattleOver, phaseReason);

		return orchestrator;
	}

	internal void EnterPlayerTurn(string reason = "player turn")
	{
		PrepareRound();
		EnsureAgentInitialized(ActivePlayerId);
		SetPhase(EBattlePhase.PlayerTurn, reason);
	}

	internal void GrantPlayerCanWork() => SetAgentCanWork(ActivePlayerId, true);

	internal void RevokePlayerCanWork() => SetAgentCanWork(ActivePlayerId, false);

	public Task<ActionProductionResult> WaitForBatchAsync(
		string actorId,
		CancellationToken cancellationToken = default) =>
		_actionSink.WaitForBatchAsync(actorId, cancellationToken);

	public IActionBatchWriter WriterFor(string actorId) => _actionSink.WriterFor(actorId);

	private void BeginRound(string reason)
	{
		PrepareRound();
		ContinueRound(reason);
	}

	private void ContinueRound(string reason)
	{
		var next = NextLivingActivation();
		if (next is null)
			throw new InvalidOperationException("An ongoing battle must have at least one living activation.");

		if (next.ExecutionAgent is UserExecutionAgent)
		{
			ActivePlayerId = next.State.Id;
			EnsureAgentInitialized(ActivePlayerId);
			SetPhase(EBattlePhase.PlayerTurn, reason);
			return;
		}

		SetPhase(EBattlePhase.Resolving, $"{reason}; resolving initiative leaders");
		var version = ++_resolveVersion;
		_ = ResolveSegmentAndReplay(TurnNumber, version, playerActorId: null);
	}

	private void PrepareRound()
	{
		_engine.ActorRuntimes.Reset();
		RevokeAllCanWork();
		_activatedThisRound.Clear();
	}

	private Unit? NextLivingActivation()
	{
		var units = UnitRegistry.For(_engine.World);
		foreach (var actorId in units.ActivationOrder)
		{
			if (!_activatedThisRound.Contains(actorId)
				&& units.TryGet(actorId, out var unit)
				&& unit.State.IsAlive)
				return unit;
		}

		return null;
	}

	public void EndTurn()
	{
		if (Phase != EBattlePhase.PlayerTurn)
		{
			BattleDiagnostics.LogEndTurnIgnored(Phase);
			return;
		}

		var completedTurn = TurnNumber;
		var playerActorId = ActivePlayerId;
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
		_ = ResolveSegmentAndReplay(completedTurn, version, playerActorId);
	}

	public void NotifyReplayComplete()
	{
		if (Phase != EBattlePhase.Replaying)
		{
			BattleDiagnostics.LogReplayNotifyIgnored(Phase);
			return;
		}

		var roundEnded = TurnNumber > PendingReplayTurn;
		PendingReplay = null;
		if (IsBattleOver)
		{
			SetPhase(EBattlePhase.BattleOver, "battle over after replay");
			return;
		}

		if (roundEnded)
			BeginRound("next round");
		else
			ContinueRound("replay complete");
	}

	public void Retire()
	{
		if (Phase is EBattlePhase.BattleOver)
			return;

		_resolveVersion++;
		_engine.Commit(CommitBattleOutcomeDef.Instance.BindRetire());
		SetPhase(EBattlePhase.BattleOver, "retired");
	}

	public void ForceOutcome(EBattleResult result)
	{
		if (result is not (EBattleResult.Win or EBattleResult.Lose))
			throw new ArgumentOutOfRangeException(nameof(result), result, "Only win or lose can be forced.");
		if (!CanForceOutcome)
			throw new InvalidOperationException($"Cannot force an outcome during phase {Phase}.");

		_engine.Commit(CommitBattleOutcomeDef.Instance.BindForce(result, ActivePlayerId));
		SetPhase(EBattlePhase.BattleOver, $"debug forced {result.ToString().ToLowerInvariant()}");
	}

	public IDisposable Subscribe<TEntry>(Action<TEntry> listener)
		where TEntry : ITimelineEntry =>
		_engine.Subscribe(listener);

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

		if (phase == EBattlePhase.PlayerTurn)
		{
			NotifyWorldUpdated();
			GrantPlayerCanWork();
		}

		PhaseChanged?.Invoke(phase);
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

	private async Task<TurnReplay> ExecuteSegmentAsync(string? playerActorId)
	{
		if (_resolveInProgress)
			throw new InvalidOperationException("Cannot resolve an activation segment while another resolve is running.");

		_resolveInProgress = true;
		try
		{
			var turnNumber = TurnNumber;
			var historyStart = _engine.History(turnNumber).Count;
			var segmentStart = SnapshotAll();

			while (NextLivingActivation() is { } live)
			{
				var actorId = live.State.Id;
				if (live.ExecutionAgent is UserExecutionAgent
					&& !string.Equals(actorId, playerActorId, StringComparison.Ordinal))
					break;

				EnsureAgentInitialized(actorId);
				var batch = await TakeActorBatchAsync(actorId);
				CommitActor(actorId, batch.Actions);
				_activatedThisRound.Add(actorId);
				playerActorId = null;
				NotifyWorldUpdated();
			}

			var roundEnded = NextLivingActivation() is null;
			if (roundEnded)
			{
				CommitRoundUpkeep();
				_engine.Commit(CommitBattleOutcomeDef.Instance.BindEvaluate());
			}

			var history = _engine.History(turnNumber)
				.Skip(historyStart)
				.ToArray();
			var segmentEnd = SnapshotAll();
			if (roundEnded)
				_engine.AdvanceTick();

			return new TurnReplay(segmentStart, history, segmentEnd);
		}
		finally
		{
			_resolveInProgress = false;
		}
	}

	private async Task ResolveSegmentAndReplay(
		int completedTurn,
		int version,
		string? playerActorId)
	{
		var resolveTimer = Stopwatch.StartNew();
		try
		{
			var replay = await ExecuteSegmentAsync(playerActorId);
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
			PendingReplay = replay;
			PendingReplayTurn = completedTurn;
			SetPhase(EBattlePhase.Replaying, $"turn {completedTurn} resolved");
			TurnResolved?.Invoke(replay, completedTurn);
		}
		catch (Exception ex) when (version == _resolveVersion && Phase == EBattlePhase.Resolving)
		{
			BattleDiagnostics.LogJobFailed(ex);
			throw new InvalidOperationException("Activation segment resolve failed after commit.", ex);
		}
	}

	public void Dispose() => _engine.Dispose();
}
