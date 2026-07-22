using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Tests;

internal sealed class TestPlan
{
	private readonly Simulation<BattleBoard, ActorSession> _session;

	private TestPlan(
		string ownerId,
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		IReadOnlySet<Coord> blocked,
		Simulation<BattleBoard, ActorSession> session)
	{
		OwnerId = ownerId;
		Player = player;
		Enemy = enemy;
		Grid = grid;
		BlockedCells = blocked;
		_session = session;
	}

	public string OwnerId { get; }
	public Unit Player { get; }
	public Unit Enemy { get; }
	public BoundedGrid Grid { get; }
	public IReadOnlySet<Coord> BlockedCells { get; }
	public BattleBoard Board => _session.PreviewWorld;
	public ActorSession Runtime => _session.PreviewRuntime;
	public IReadOnlyList<IAction> Actions => _session.Actions;
	public Simulation<BattleBoard, ActorSession> Session => _session;
	public int MissilesRemainingThisTurn => Board.StateOf(OwnerId).MissilesRemaining;

	public static TestPlan Begin(string ownerId, Coord origin, int momentum = 0)
	{
		var player = BattleTestFixture.Player(origin, momentum: momentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		return Begin(
			ownerId,
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });
	}

	public static TestPlan Begin(
		string ownerId,
		Unit player,
		Unit enemy,
		BoundedGrid grid,
		IReadOnlySet<Coord> blocked)
	{
		var board = BattleBoard.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			grid,
			blocked);
		var session = new Simulation<BattleBoard, ActorSession>(board, new ActorSession());
		session.Begin(0);
		BattleOrchestrator.ApplyEndOfPhase(session.PreviewWorld, session.PreviewRuntime, ownerId);
		return new TestPlan(ownerId, player, enemy, grid, blocked, session);
	}

	public void BeginTurn(int tick)
	{
		_session.Begin(tick);
		BattleOrchestrator.ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
	}

	public bool TryApplyAndEnqueue(IAction action)
	{
		if (!_session.TryEnqueue(action))
			return false;

		BattleOrchestrator.ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
		return true;
	}

	public void ForceApplyAndEnqueue(IAction action)
	{
		_session.ForceEnqueue(action);
		_session.Refresh();
		BattleOrchestrator.ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
	}

	public bool TryEnqueue(IAction action) => TryApplyAndEnqueue(action);

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
			if (!TryApplyAndEnqueue(step with { UndoGroup = undoGroup }))
				return false;
		}

		return true;
	}

	public void EnqueueMovePath(Option option)
	{
		if (!TryEnqueueMovePath(option))
			throw new InvalidOperationException("Failed to enqueue move path.");
	}

	public bool TryUndoLast()
	{
		if (!_session.TryUndoLast())
			return false;

		BattleOrchestrator.ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewRuntime, OwnerId);
		return true;
	}

	public int TurnStartTick => _session.AnchorTick;

	public void AdvanceToTick(int tick) =>
		_session.AdvanceToTick(tick, action =>
		{
			if (action is IAction<BattleBoard, ActorSession> typed)
				typed.Apply(_session.PreviewWorld, _session.PreviewRuntime);
		});

	public CommittedPlan FinalizePlan() => new(Actions.ToList());
}

internal readonly record struct CommittedPlan(IReadOnlyList<IAction> Actions);
