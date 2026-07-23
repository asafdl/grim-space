using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Debug;
using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Ids;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using UnitState = GrimSpace.Battle.Units.State;
using BattleSimulation = GrimSpace.Core.Engine.Simulation<
	GrimSpace.Battle.Board.BattleBoard,
	GrimSpace.Battle.Runtime.ActorSession>;

namespace GrimSpace.Battle;

public sealed class BattleOrchestrator
{
	private readonly Engine<BattleBoard, ActorSession> _engine;
	private readonly Unit _player;
	private readonly Unit _enemy;
	private readonly IReadOnlyList<Unit> _roster;
	private readonly HazardSystem _hazards;

	private BattleSimulation _session = null!;

	public BattleOrchestrator(
		Engine<BattleBoard, ActorSession> engine,
		IReadOnlyList<Unit> roster,
		Unit player,
		Unit enemy,
		HazardSystem hazards)
	{
		_engine = engine;
		_roster = roster;
		_player = player;
		_enemy = enemy;
		_hazards = hazards;
	}

	public Engine<BattleBoard, ActorSession> Engine => _engine;
	public BoundedGrid Grid => _engine.World.Grid;
	public IReadOnlyList<Unit> Units => _roster;
	public HazardSystem Hazards => _hazards;
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }
	public bool IsResolving { get; private set; }
	public int TurnNumber { get; private set; } = 1;
	public string? ActiveUnitId { get; private set; }

	public BattleSimulation Session => _session;
	public string PlayerId => _player.State.Id;
	public Unit Opponent => _enemy;
	public BattleBoard Board => _session.PreviewWorld;
	public ActorSession Runtime => _session.PreviewActorRuntimes.For(PlayerId);
	public IReadOnlyList<IAction> Actions => _session.Actions;
	public int MissilesRemainingThisTurn => Board.StateOf(PlayerId).MissilesRemaining;

	public static BattleOrchestrator FromEncounter(Encounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var timeline = new Timeline();
		var hazards = new HazardSystem();
		var ids = new UnitIdRegistry();

		hazards.RegisterBoard(
			encounter.BoardHazards.Select(spawn =>
				Hazard.Asteroid(
					ids.NextNonUnitId("asteroid"),
					spawn.Center,
					grid,
					spawn.Radius,
					spawn.VisualId)));

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(spawn.Unit, spawn.Position, ids, spawn.InitialMomentum))
			.ToArray();

		var player = units.First(u => u.Controller == EController.Player);
		var enemy = units.First(u => u.Controller == EController.Enemy);
		var blockedCells = hazards.GetBlockedCells();
		var world = BattleBoard.FromLive(units, hazards.MutableNonUnits, grid, blockedCells, timeline);

		var actorRuntimes = new ActorRuntimes<ActorSession>();
		actorRuntimes.For(player.State.Id);
		actorRuntimes.For(enemy.State.Id);
		actorRuntimes.For(EntityIds.System);

		var engine = new Engine<BattleBoard, ActorSession>(world, actorRuntimes);
		var orchestrator = new BattleOrchestrator(engine, units, player, enemy, hazards);

		orchestrator.SetActiveUnit(player.State.Id);
		orchestrator.BeginTurn();
		return orchestrator;
	}

	public Unit? GetPlayer() =>
		Units.FirstOrDefault(u => u.Controller == EController.Player);

	public Unit? GetEnemy() =>
		Units.FirstOrDefault(u => u.Controller == EController.Enemy);

	public void SetActiveUnit(string unitId) => ActiveUnitId = unitId;

	public bool IsActive(string unitId) => ActiveUnitId == unitId;

	public void BeginTurn()
	{
		_session = _engine.CreateSimulation();
		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewActorRuntimes.For(PlayerId), PlayerId);
	}

	public bool CanAct(Unit unit) =>
		!IsBattleOver && !IsResolving && IsActive(unit.State.Id) && unit.State.IsAlive;

	public Unit? GetActiveActor() =>
		GetActiveUnits().FirstOrDefault(u => u.Controller == EController.Player);

	public bool IsLegal(IAction action)
	{
		var player = GetActiveActor();
		if (player is null || !CanAct(player))
			return false;

		if (action is not IAction<BattleBoard, ActorSession> typed)
			return false;

		var runtime = _session.PreviewActorRuntimes.For(action);
		return typed.Definition.IsLegal(action, Board, runtime);
	}

	public bool TryEnqueue(IAction action)
	{
		var player = GetActiveActor();
		if (player is null || !CanAct(player))
			return false;

		if (action is FlakAction && _session.Actions.Any(queued => queued is FlakAction))
			return false;

		if (action is RailgunAction && _session.Actions.Any(queued => queued is RailgunAction))
			return false;

		if (!_session.TryEnqueue(action))
			return false;

		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewActorRuntimes.For(PlayerId), PlayerId);
		return true;
	}

	public bool TryEnqueueMovePath(Option option)
	{
		var actor = Board.StateOf(PlayerId);
		IReadOnlyList<MoveStepAction> steps;
		try
		{
			steps = MoveDef.StepsFromPath(PlayerId, BodyFrame.From(actor), actor.Position, option.Path);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		var undoGroup = _session.AllocateUndoGroup();
		foreach (var step in steps)
		{
			if (!_session.TryEnqueue(step with { UndoGroup = undoGroup }))
				return false;
		}

		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewActorRuntimes.For(PlayerId), PlayerId);
		return true;
	}

	public bool TryUndoLast()
	{
		if (!_session.TryUndoLast())
			return false;

		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewActorRuntimes.For(PlayerId), PlayerId);
		return true;
	}

	public bool ResolveTurn(IReadOnlyList<IAction> playerActions, IPresentationEventSink? sink = null)
	{
		if (IsBattleOver || IsResolving)
			return false;

		IsResolving = true;
		try
		{
			var result = ExecuteTurn(playerActions, sink);
			if (!result.Success)
				return false;

			IsBattleOver = result.IsBattleOver;
			WinnerId = result.WinnerId;

			if (GetPlayer() is not null)
				BeginTurn();

			return true;
		}
		finally
		{
			IsResolving = false;
		}
	}

	public static void ApplyEndOfPhase(BattleBoard world, ActorSession runtime, string actorId)
	{
		var action = EndOfPhaseDef.Instance.Bind(actorId);
		foreach (var effect in action.Definition.Resolve(action, world, runtime))
			effect.Apply(world, runtime, action.ActorId);
	}

	public static bool TryEnqueueMovePath(BattleSimulation session, string actorId, Option option)
	{
		var actor = session.PreviewWorld.StateOf(actorId);
		IReadOnlyList<MoveStepAction> steps;
		try
		{
			steps = MoveDef.StepsFromPath(actorId, BodyFrame.From(actor), actor.Position, option.Path);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		var undoGroup = session.AllocateUndoGroup();
		foreach (var step in steps)
		{
			if (!session.TryEnqueue(step with { UndoGroup = undoGroup }))
				return false;
		}

		return true;
	}

	private PipelineResult ExecuteTurn(IReadOnlyList<IAction> playerActions, IPresentationEventSink? sink)
	{
		var turnNumber = TurnNumber;
		var unitsAtTurnStart = SnapshotAll();
		var turnStart = _engine.World.Timeline.Clock.Current;
		var hazardsBeforeResolve = Hazards.Active.ToList();
		var applied = new List<IAction>();
		IReadOnlyDictionary<string, UnitState>? unitsAfterPlayer = null;

		_engine.ActorRuntimes.Reset();

		if (!TrySchedulePlayerPhase(_player.State.Id, playerActions, TurnPhases.Player))
			return new PipelineResult(false, null, Success: false);

		foreach (var tick in _engine.Step(TurnPhases.Player))
		{
			CollectTick(tick, sink, applied);
			if (tick.Tick == turnStart + TurnPhases.Player)
				unitsAfterPlayer = SnapshotAll();
		}

		var enemySim = _engine.CreateSimulation();
		ApplyEndOfPhase(
			enemySim.PreviewWorld,
			enemySim.PreviewActorRuntimes.For(_enemy.State.Id),
			_enemy.State.Id);
		var enemyActions = EnemyPlanner.PlanTurn(enemySim, _enemy);

		SchedulePhase(_enemy.State.Id, enemyActions, TurnPhases.Enemy - TurnPhases.Player);
		foreach (var tick in _engine.Step(TurnPhases.Enemy - TurnPhases.Player))
			CollectTick(tick, sink, applied);

		ScheduleRoundUpkeep(TurnPhases.End - TurnPhases.Enemy);
		foreach (var tick in _engine.Step(TurnPhases.End - TurnPhases.Enemy))
			CollectTick(tick, sink, applied);

		var outcome = RulesEngine.Evaluate(Units);
		FinalizeRound();

		StateLog.LogTurnResolution(
			turnNumber,
			applied,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			unitsAfterPlayer ?? SnapshotAll(),
			SnapshotAll());

		return new PipelineResult(outcome.IsOver, outcome.WinnerId, Success: true);
	}

	private static void CollectTick(TickResult tick, IPresentationEventSink? sink, List<IAction> applied)
	{
		foreach (var action in tick.AppliedActions)
			sink?.OnActionApplied(new PresentationEvent(action));

		applied.AddRange(tick.AppliedActions);
	}

	private bool TrySchedulePlayerPhase(string actorId, IReadOnlyList<IAction> actions, int delayTicks)
	{
		if (!_engine.TryScheduleFromSimulation(_session, out _session, actions, delayTicks))
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
		foreach (var unit in _roster)
			_engine.ScheduleToWorldTimeline(new RoundUpkeepAction(unit.State.Id), delayTicks);

		_engine.ScheduleToWorldTimeline(new ClearTurnHazardsAction(), delayTicks);
	}

	private void FinalizeRound()
	{
		TurnNumber++;

		var player = GetPlayer();
		if (player is not null)
			SetActiveUnit(player.State.Id);
	}

	private IEnumerable<Unit> GetActiveUnits() =>
		Units.Where(u => IsActive(u.State.Id) && u.State.IsAlive);

	private Dictionary<string, UnitState> SnapshotAll() =>
		Units.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());

	private readonly record struct PipelineResult(bool IsBattleOver, string? WinnerId, bool Success = true);
}
