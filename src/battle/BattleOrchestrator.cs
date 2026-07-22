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
	public BoundedGrid Grid { get; }
	public Timeline Timeline { get; }
	public IReadOnlyList<Unit> Units { get; }
	public HazardSystem Hazards { get; }
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }
	public bool IsResolving { get; private set; }
	public int TurnNumber { get; private set; } = 1;
	public string? ActiveUnitId { get; private set; }

	private readonly Unit _player;
	private readonly Unit _enemy;
	private readonly IReadOnlyList<Unit> _roster;
	private readonly IReadOnlyDictionary<string, NonUnit> _nonUnits;
	private readonly IReadOnlySet<Coord> _blockedCells;

	private BattleSimulation _session = null!;

	public BattleOrchestrator(
		BoundedGrid grid,
		Timeline timeline,
		IReadOnlyList<Unit> units,
		Unit player,
		Unit enemy,
		HazardSystem hazards,
		IReadOnlySet<Coord> blockedCells)
	{
		Grid = grid;
		Timeline = timeline;
		Units = units;
		_player = player;
		_enemy = enemy;
		_roster = units;
		_nonUnits = hazards.NonUnits;
		_blockedCells = blockedCells;
		Hazards = hazards;
	}

	public BattleSimulation Session => _session;
	public string OwnerId => _player.State.Id;
	public Unit Opponent => _enemy;
	public BattleBoard Board => _session.PreviewWorld;
	public ActorSession Runtime => _session.PreviewRuntime;
	public IReadOnlyList<IAction> Actions => _session.Actions;
	public int MissilesRemainingThisTurn => Board.StateOf(OwnerId).MissilesRemaining;

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

		var orchestrator = new BattleOrchestrator(grid, timeline, units, player, enemy, hazards, blockedCells);

		if (player is not null)
			orchestrator.SetActiveUnit(player.State.Id);

		orchestrator.BeginTurn(timeline.Clock.Current);
		return orchestrator;
	}

	public Unit? GetPlayer() =>
		Units.FirstOrDefault(u => u.Controller == EController.Player);

	public Unit? GetEnemy() =>
		Units.FirstOrDefault(u => u.Controller == EController.Enemy);

	public void SetActiveUnit(string unitId) => ActiveUnitId = unitId;

	public bool IsActive(string unitId) => ActiveUnitId == unitId;

	public void BeginTurn(int turnStartTick)
	{
		var anchor = BattleBoard.FromSnapshot(_roster, _nonUnits, Grid, _blockedCells);
		_session = new BattleSimulation(anchor, new ActorSession());
		_session.Begin(turnStartTick);
		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
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

		return typed.Definition.IsLegal(action, Board, Runtime);
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

		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
		return true;
	}

	public bool TryEnqueueMovePath(Option option)
	{
		var actor = Board.StateOf(OwnerId);
		IReadOnlyList<MoveStepAction> steps;
		try
		{
			steps = MoveDef.StepsFromPath(OwnerId, BodyFrame.From(actor), actor.Position, option.Path);
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

		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
		return true;
	}

	public bool TryUndoLast()
	{
		if (!_session.TryUndoLast())
			return false;

		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
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
			IsBattleOver = result.IsBattleOver;
			WinnerId = result.WinnerId;

			if (GetPlayer() is not null)
				BeginTurn(Timeline.Clock.Current);

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
			effect.Apply(world, runtime, action.OwnerId);
	}

	public static bool TryEnqueueMovePath(BattleSimulation session, string ownerId, Option option)
	{
		var actor = session.PreviewWorld.StateOf(ownerId);
		IReadOnlyList<MoveStepAction> steps;
		try
		{
			steps = MoveDef.StepsFromPath(ownerId, BodyFrame.From(actor), actor.Position, option.Path);
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
		var turnStart = Timeline.Clock.Current;

		var enemyActions = CommitToTimeline(playerActions, turnStart);
		var execution = ExecuteTimeline(turnStart, sink);
		var outcome = RulesEngine.Evaluate(Units);

		var hazardsBeforeResolve = Hazards.Active.ToList();

		Timeline.Clock.Set(turnStart + TurnPhases.End);
		FinalizeRound();

		StateLog.LogTurnResolution(
			turnNumber,
			execution.Applied,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			execution.UnitsAfterPlayer,
			SnapshotAll());

		return new PipelineResult(outcome.IsOver, outcome.WinnerId);
	}

	private IReadOnlyList<IAction> CommitToTimeline(IReadOnlyList<IAction> playerActions, int turnStart)
	{
		var resolvedHazardCells = EnemyPlanner.CollectHazardCells(
			Hazards.GetOccupiedCells(),
			_player,
			Units,
			Grid,
			_nonUnits,
			_blockedCells,
			playerActions,
			turnStart);

		var enemyActions = EnemyPlanner.PlanTurn(
			_enemy,
			Units,
			Grid,
			_nonUnits,
			resolvedHazardCells,
			_blockedCells,
			turnStart);

		EnqueuePhase(Timeline, turnStart + TurnPhases.Player, _player.State.Id, playerActions);
		EnqueuePhase(Timeline, turnStart + TurnPhases.Enemy, _enemy.State.Id, enemyActions);
		EnqueueRoundUpkeep(Timeline, turnStart + TurnPhases.End, Units);

		return enemyActions;
	}

	private TurnExecutionResult ExecuteTimeline(int turnStart, IPresentationEventSink? sink)
	{
		var applied = new List<IAction>();
		IReadOnlyDictionary<string, UnitState>? unitsAfterPlayer = null;

		var playerSession = new ActorSession();
		var enemySession = new ActorSession();

		var tick = turnStart;
		while (tick <= Timeline.MaxTick)
		{
			Timeline.Clock.Set(tick);

			while (Timeline.At(tick).TryDequeue(out var action) && action is not null)
			{
				if (SystemAction.Is(action))
					ApplySystemAction(action);
				else
					ApplyUnitAction(action, playerSession, enemySession);

				applied.Add(action);
				sink?.OnActionApplied(new PresentationEvent(action));
			}

			if (tick == turnStart + TurnPhases.Player)
				unitsAfterPlayer = SnapshotAll();

			tick++;
		}

		return new TurnExecutionResult(applied, unitsAfterPlayer ?? SnapshotAll());
	}

	private void ApplySystemAction(IAction action)
	{
		if (action is not IAction<BattleBoard, ActorSession> typed)
			return;

		var board = BattleBoard.FromLive(
			Units,
			Hazards.MutableNonUnits,
			Grid,
			Hazards.GetBlockedCells(),
			Timeline);
		foreach (var effect in typed.Definition.Resolve(action, board, new ActorSession()))
			effect.Apply(board, new ActorSession(), action.OwnerId);
	}

	private void ApplyUnitAction(
		IAction action,
		ActorSession playerSession,
		ActorSession enemySession)
	{
		if (action is not IAction<BattleBoard, ActorSession> typed)
			return;

		var ownerId = action.OwnerId;
		var session = ownerId == _player.State.Id ? playerSession : enemySession;

		var board = BattleBoard.FromLive(
			Units,
			Hazards.MutableNonUnits,
			Grid,
			Hazards.GetBlockedCells(),
			Timeline);
		foreach (var effect in typed.Definition.Resolve(action, board, session))
			effect.Apply(board, session, action.OwnerId);
	}

	private void FinalizeRound()
	{
		TurnNumber++;

		var player = GetPlayer();
		if (player is not null)
			SetActiveUnit(player.State.Id);
	}

	private static void EnqueuePhase(
		Timeline timeline,
		int tick,
		string actorId,
		IReadOnlyList<IAction> actions)
	{
		timeline.At(tick).EnqueueAll(actions);
		timeline.At(tick).Enqueue(new EndOfPhaseAction(actorId));
	}

	private static void EnqueueRoundUpkeep(Timeline timeline, int tick, IReadOnlyList<Unit> units)
	{
		foreach (var unit in units)
			timeline.At(tick).Enqueue(new RoundUpkeepAction(unit.State.Id));

		timeline.At(tick).Enqueue(new ClearTurnHazardsAction());
	}

	private IEnumerable<Unit> GetActiveUnits() =>
		Units.Where(u => IsActive(u.State.Id) && u.State.IsAlive);

	private Dictionary<string, UnitState> SnapshotAll() =>
		Units.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());

	private readonly record struct PipelineResult(bool IsBattleOver, string? WinnerId);

	private sealed record TurnExecutionResult(
		IReadOnlyList<IAction> Applied,
		IReadOnlyDictionary<string, UnitState> UnitsAfterPlayer);
}
