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

public sealed class BattleOrchestrator
{
	private readonly Engine<BattleWorld, ActorRuntime> _engine;
	private readonly Manager _objectives;

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
	public string? ActiveUnitId { get; private set; }
	public EBattlePhase Phase { get; private set; }

	public bool AcceptsPlayerInput => Phase == EBattlePhase.PlayerTurn;

	public event Action<string?>? ActiveUnitChanged;
	public event Action<EBattlePhase>? PhaseChanged;
	public event Action<TurnReplay, int>? TurnResolved;

	internal void RegisterActiveUnitChanged(Action<string?> handler) =>
		ActiveUnitChanged += handler;

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
			unit.ExecutionAgent.Init(unit.State.Id, orchestrator.Engine.CreateSimulation, orchestrator.RegisterActiveUnitChanged);

		orchestrator.SetActive(player.State.Id);
		orchestrator.SetPhase(EBattlePhase.PlayerTurn, "encounter ready");
		return orchestrator;
	}

	public void SetActive(string? unitId)
	{
		if (ActiveUnitId == unitId)
			return;

		ActiveUnitId = unitId;
		ActiveUnitChanged?.Invoke(unitId);
	}

	public bool IsActive(string unitId) => ActiveUnitId == unitId;

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

		SetActive(PlayerId);
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

	public Unit? GetActiveUnit()
	{
		if (ActiveUnitId is not string id
			|| !UnitRegistry.For(_engine.World).TryGet(id, out var unit))
		{
			return null;
		}

		return unit;
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
		var hazardsBeforeResolve = _engine.World.TurnHazards.ToList();
		IReadOnlyDictionary<string, UnitState>? unitsAfterPlayer = null;

		_engine.ActorRuntimes.Reset();

		var units = UnitRegistry.For(_engine.World);
		for (var node = units.First; node is not null; node = node.Next)
		{
			if (!units.TryGet(node.Value, out var live) || !live.State.IsAlive)
				continue;

			live.ExecutionAgent.Init(live.State.Id, _engine.CreateSimulation, RegisterActiveUnitChanged);
			SetActive(live.State.Id);
			var planned = await live.ExecutionAgent.GetActions();
			CommitActor(live.State.Id, planned);

			if (live.State.Id == PlayerId)
				unitsAfterPlayer = SnapshotAll();
		}

		CommitRoundUpkeep();
		var history = _engine.History();
		_engine.AdvanceTick();
		SetActive(null);

		GameLog.Log(
			$"Turn {turnNumber} sim: "
			+ $"total={resolveTimer.Elapsed.TotalMilliseconds:F1}ms "
			+ $"history={history.Count}");

		var endStates = SnapshotAll();
		StateLog.LogTurnResolution(
			turnNumber,
			history,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			unitsAfterPlayer ?? endStates,
			endStates,
			id => ActionLog.DisplayName(units, id));

		return new TurnReplay(unitsAtTurnStart, history, endStates);
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
		batch.Add(new ClearTurnHazardsAction());
		return _engine.Commit([..batch]);
	}

	private Dictionary<string, UnitState> SnapshotAll() =>
		UnitRegistry.For(_engine.World).All.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());

	private void SetPhase(EBattlePhase phase, string reason)
	{
		if (Phase == phase)
			return;

		var from = Phase;
		Phase = phase;
		BattleDiagnostics.LogPhaseTransition(from, phase, reason);
		PhaseChanged?.Invoke(phase);
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
			SetActive(null);
			TurnResolved?.Invoke(replay, completedTurn);
		}
		catch (Exception ex) when (version == _resolveVersion && Phase == EBattlePhase.Resolving)
		{
			BattleDiagnostics.LogJobFailed(ex);
			throw new InvalidOperationException("Turn resolve failed after commit.", ex);
		}
	}
}
