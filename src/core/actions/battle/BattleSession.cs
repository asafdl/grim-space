using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class SimulatedTurn
{
	public required BattleBoard Board { get; init; }
	public required string ActorId { get; init; }

	public State Actor => Board.StateOf(ActorId);

	public IEnumerable<Hazard> Hazards => Board.TurnHazards;
}

/// <summary>
/// Battle planning session: composes engine Simulator with board snapshot and replay apply.
/// </summary>
public sealed class BattleSession
{
	private readonly Simulator<BattleBoard, TurnState> _sim = new(() => new TurnState());
	private readonly List<IAction> _appliedActions = [];
	private string? _ownerId;
	private IReadOnlyList<Unit>? _roster;
	private BoundedGrid? _grid;
	private IReadOnlySet<Coord>? _blockedCells;
	private IReadOnlyDictionary<string, NonUnit>? _nonUnits;

	public IReadOnlyList<IAction> Actions => _sim.Actions;

	public Timeline PreviewTimeline => _sim.PreviewTimeline;

	public int TurnStartTick => _sim.AnchorTick;

	public BattleBoard Board =>
		_roster is null
			? throw new InvalidOperationException("Call BeginTurn before planning.")
			: _sim.World;

	public BattlePlanContext Context => new(_appliedActions, _sim.State);

	public string? OwnerId => _ownerId;

	public void BeginTurn(
		string ownerId,
		IReadOnlyList<Unit> roster,
		BoundedGrid grid,
		IReadOnlyDictionary<string, NonUnit> nonUnits,
		IReadOnlySet<Coord> blockedCells,
		int turnStartTick)
	{
		_ownerId = ownerId;
		_roster = roster;
		_grid = grid;
		_nonUnits = nonUnits;
		_blockedCells = blockedCells;

		_sim.Begin(turnStartTick);
		_sim.State.Clear();
		_appliedActions.Clear();
		_sim.SetWorld(BattleBoard.FromSnapshot(roster, nonUnits, grid, blockedCells));
	}

	public void CopyFrom(IEnumerable<IAction> actions)
	{
		_sim.CopyActionsFrom(actions);
		_sim.State.Clear();
		_appliedActions.Clear();
		_ownerId = null;
		_roster = null;
		_grid = null;
		_blockedCells = null;
		_nonUnits = null;
		_sim.SetWorld(default!);
	}

	public bool TryApplyAndEnqueue(IAction action)
	{
		EnsureBoard();
		if (!action.IsLegal(Board, Context))
			return false;

		_sim.Enqueue(action);
		Replay();
		return true;
	}

	public void ForceApplyAndEnqueue(IAction action)
	{
		EnsureBoard();
		_sim.Enqueue(action);
		Replay();
	}

	public void EnqueueMovePath(string actorId, Option option)
	{
		EnsureBoard();
		var origin = Board.StateOf(actorId).Position;
		var frame = BodyFrame.From(Board.StateOf(actorId));
		var steps = MoveStepAction.BuildSteps(actorId, frame, origin, option.Path);
		var group = _sim.AllocateUndoGroup();

		foreach (var step in steps)
			_sim.Enqueue(new MoveStepAction(
				step.OwnerId,
				step.From,
				step.To,
				step.UsedDirectionsMaskBefore,
				group));

		Replay();
	}

	public bool TryEnqueueMovePath(string actorId, Option option)
	{
		if (_sim.State.IsMovePathStarted)
			return false;

		EnqueueMovePath(actorId, option);
		return true;
	}

	public bool TryUndoLast()
	{
		if (!_sim.TryUndoLast())
			return false;

		Replay();
		return true;
	}

	public SimulatedTurn GetPreview(string actorId) =>
		new()
		{
			Board = Board,
			ActorId = actorId,
		};

	public void AdvanceToTick(int tick)
	{
		EnsureTurnContext();
		_sim.AdvanceToTick(tick, scheduled =>
		{
			var applied = new List<IAction>();
			var context = new BattlePlanContext(applied, _sim.State);
			ActionApplicator.TryApplyOne(
				scheduled,
				Board,
				context,
				_sim.PreviewTimeline,
				_ownerId!,
				checkLegal: false);
		});
	}

	private void Replay()
	{
		EnsureTurnContext();
		_sim.SetWorld(BattleBoard.FromSnapshot(_roster!, _nonUnits!, _grid!, _blockedCells!));
		_sim.ResetPreviewFork();
		_sim.State.Clear();
		_appliedActions.Clear();
		ActionApplicator.TryApplyAll(
			ActionApplicator.WithPhaseEnd(Actions, _ownerId),
			Board,
			Context,
			_sim.PreviewTimeline,
			_ownerId!);
	}

	private void EnsureBoard()
	{
		if (_roster is null)
			throw new InvalidOperationException("Call BeginTurn before planning.");
	}

	private void EnsureTurnContext()
	{
		if (_ownerId is null || _roster is null || _grid is null || _blockedCells is null || _nonUnits is null)
			throw new InvalidOperationException("Call BeginTurn before planning.");
	}
}
