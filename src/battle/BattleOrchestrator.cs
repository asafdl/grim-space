using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Debug;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle;

/// <summary>
/// Turn coordinator for a battle: owns the live engine, exposes preview <see cref="Sim"/>,
/// and frozen <see cref="Layout"/> for scene setup. Does not mirror world units or hazards.
/// </summary>
public sealed class BattleOrchestrator
{
	private readonly Engine<BattleWorld, ActorRuntime> _engine;
	private readonly string _opponentId;

	private BattleSimulation _sim = null!;

	internal BattleOrchestrator(
		Engine<BattleWorld, ActorRuntime> engine,
		BattleLayout layout,
		string playerId,
		string opponentId)
	{
		_engine = engine;
		Layout = layout;
		PlayerId = playerId;
		_opponentId = opponentId;
	}

	internal Engine<BattleWorld, ActorRuntime> Engine => _engine;

	public BattleLayout Layout { get; }
	public BattleSimulation Sim => _sim;
	public string PlayerId { get; }
	public string OpponentId => _opponentId;
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }
	public bool IsResolving { get; private set; }
	public int TurnNumber { get; private set; } = 1;
	public string? ActiveUnitId { get; private set; }

	public MoveUi MoveUi { get; private set; } = null!;

	public IReadOnlyDictionary<string, UnitState> LiveUnitStates =>
		_engine.World.Units.ToDictionary(pair => pair.Key, pair => pair.Value.State.Clone());

	public static BattleOrchestrator FromEncounter(Encounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var timeline = new Timeline();
		var nonUnits = new Dictionary<string, NonUnit>();
		var ids = new UnitIdRegistry();

		foreach (var spawn in encounter.WorldHazards)
		{
			var hazard = Hazard.Asteroid(
				ids.NextNonUnitId("asteroid"),
				spawn.Center,
				grid,
				spawn.Radius,
				spawn.VisualId);
			nonUnits[hazard.Id] = hazard;
		}

		var terrainHazards = nonUnits.Values.OfType<Hazard>().ToList();
		var blockedCells = BattleWorld.TerrainBlockedCells(terrainHazards);

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(spawn.Unit, spawn.Position, ids, spawn.InitialMomentum))
			.ToArray();

		var playerId = units.First(unit => unit.Controller == EController.Player).State.Id;
		var opponentId = units.First(unit => unit.Controller == EController.Enemy).State.Id;
		var world = BattleWorld.FromLive(units, nonUnits, grid, blockedCells, timeline);
		var layout = BattleLayout.FromEncounter(grid, terrainHazards, units);

		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		actorRuntimes.For(playerId);
		actorRuntimes.For(opponentId);
		actorRuntimes.For(EntityIds.System);

		var engine = new Engine<BattleWorld, ActorRuntime>(world, actorRuntimes);
		var orchestrator = new BattleOrchestrator(engine, layout, playerId, opponentId);

		orchestrator.SetActiveUnit(playerId);
		orchestrator.BeginTurn();
		return orchestrator;
	}

	public void SetActiveUnit(string unitId) => ActiveUnitId = unitId;

	public bool IsActive(string unitId) => ActiveUnitId == unitId;

	public void BeginTurn()
	{
		_sim = _engine.CreateSimulation();
		MoveUi = MoveUi.Build(this);
	}

	public bool CanAct(Unit unit) =>
		!IsBattleOver && !IsResolving && IsActive(unit.State.Id) && unit.State.IsAlive;

	public Unit? GetActiveActor()
	{
		if (ActiveUnitId is not string id
			|| !_engine.World.Units.TryGetValue(id, out var unit)
			|| unit.Controller != EController.Player)
		{
			return null;
		}

		return unit;
	}

	public TurnReplay ResolveTurn(IReadOnlyList<IAction> playerActions)
	{
		if (IsBattleOver || IsResolving)
			throw new InvalidOperationException("Cannot resolve turn while battle is over or already resolving.");

		IsResolving = true;
		try
		{
			var replay = ExecuteTurn(playerActions);
			var outcome = EvaluateBattleOutcome();
			IsBattleOver = outcome.IsOver;
			WinnerId = outcome.WinnerId;

			if (_engine.World.Units.TryGetValue(PlayerId, out var player) && player.State.IsAlive)
				BeginTurn();

			return replay;
		}
		finally
		{
			IsResolving = false;
		}
	}

	private TurnReplay ExecuteTurn(IReadOnlyList<IAction> playerActions)
	{
		var turnNumber = TurnNumber;
		var unitsAtTurnStart = SnapshotAll();
		var turnStart = _engine.World.Timeline.Clock.Current;
		var hazardsBeforeResolve = _engine.World.TurnHazards.ToList();
		var applied = new List<IAction>();
		IReadOnlyDictionary<string, UnitState>? unitsAfterPlayer = null;

		_engine.ActorRuntimes.Reset();

		if (!TrySchedulePlayerPhase(PlayerId, playerActions, TurnPhases.Player))
			throw new InvalidOperationException("Failed to schedule player phase onto live timeline.");

		foreach (var tick in _engine.Step(TurnPhases.Player))
		{
			CollectTick(tick, applied);
			if (tick.Tick == turnStart + TurnPhases.Player)
				unitsAfterPlayer = SnapshotAll();
		}

		var enemySim = _engine.CreateSimulation();
		var enemy = _engine.World.UnitOf(_opponentId);
		var enemyActions = EnemySimulation.BuildTurnActions(enemySim, enemy);

		SchedulePhase(_opponentId, enemyActions, TurnPhases.Enemy - TurnPhases.Player);
		foreach (var tick in _engine.Step(TurnPhases.Enemy - TurnPhases.Player))
			CollectTick(tick, applied);

		ScheduleRoundUpkeep(TurnPhases.End - TurnPhases.Enemy);
		foreach (var tick in _engine.Step(TurnPhases.End - TurnPhases.Enemy))
			CollectTick(tick, applied);

		FinalizeRound();

		var endStates = SnapshotAll();
		StateLog.LogTurnResolution(
			turnNumber,
			applied,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			unitsAfterPlayer ?? endStates,
			endStates);

		return new TurnReplay(unitsAtTurnStart, applied, endStates);
	}

	private static void CollectTick(TickResult tick, List<IAction> applied) =>
		applied.AddRange(tick.AppliedActions);

	private bool TrySchedulePlayerPhase(string actorId, IReadOnlyList<IAction> actions, int delayTicks)
	{
		if (!_engine.TryScheduleFromSimulation(_sim, out _sim, actions, delayTicks))
			return false;

		_engine.ScheduleToWorldTimeline(new EndOfPhaseAction(actorId), delayTicks);
		return true;
	}

	private void SchedulePhase(string actorId, IReadOnlyList<IAction> actions, int delayTicks)
	{
		_engine.ScheduleToWorldTimeline(actions, delayTicks);
		_engine.ScheduleToWorldTimeline(new EndOfPhaseAction(actorId), delayTicks);
	}

	private void ScheduleRoundUpkeep(int delayTicks)
	{
		foreach (var unitId in _engine.World.Units.Keys)
			_engine.ScheduleToWorldTimeline(new RoundUpkeepAction(unitId), delayTicks);

		_engine.ScheduleToWorldTimeline(new ClearTurnHazardsAction(), delayTicks);
	}

	private void FinalizeRound()
	{
		TurnNumber++;

		if (_engine.World.Units.TryGetValue(PlayerId, out var player) && player.State.IsAlive)
			SetActiveUnit(player.State.Id);
	}

	private Dictionary<string, UnitState> SnapshotAll() =>
		_engine.World.Units.ToDictionary(pair => pair.Key, pair => pair.Value.State.Clone());

	private BattleOutcome EvaluateBattleOutcome()
	{
		var units = _engine.World.Units.Values;
		var player = units.FirstOrDefault(unit => unit.Controller == EController.Player);
		var enemy = units.FirstOrDefault(unit => unit.Controller == EController.Enemy);

		if (enemy is not null && !enemy.State.IsAlive)
			return new BattleOutcome(true, player?.State.Id);

		if (player is not null && !player.State.IsAlive)
			return new BattleOutcome(true, enemy?.State.Id);

		return new BattleOutcome(false, null);
	}

	private readonly record struct BattleOutcome(bool IsOver, string? WinnerId);
}
