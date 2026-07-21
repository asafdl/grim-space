using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
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
/// Battle planning session backed by the core planning simulator.
/// </summary>
public sealed class BattleSession
{
	private readonly Simulator<BattleBoard, TurnState, BattleSlices, IBattleAction> _sim;
	private string? _ownerId;
	private IReadOnlyList<Unit>? _roster;
	private BoundedGrid? _grid;
	private IReadOnlySet<Coord>? _blockedCells;
	private IReadOnlyDictionary<string, NonUnit>? _nonUnits;

	public BattleSession()
	{
		_sim = new Simulator<BattleBoard, TurnState, BattleSlices, IBattleAction>(
			CreateWorldFromSnapshot,
			() => new TurnState(),
			BattleSliceFactory.Create,
			ExpandPlayback);
	}

	public IReadOnlyList<IBattleAction> Actions => _sim.Actions;

	public Timeline PreviewTimeline => _sim.Timeline;

	public int TurnStartTick => _sim.AnchorTick;

	public BattleBoard Board =>
		_roster is null
			? throw new InvalidOperationException("Call BeginTurn before planning.")
			: _sim.World;

	public BattlePlanContext Context => new(_sim.Actions, _sim.State);

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
	}

	public void CopyFrom(IEnumerable<IAction> actions)
	{
		_sim.CopyActionsFrom(actions.Cast<IBattleAction>());
		_ownerId = null;
		_roster = null;
		_grid = null;
		_blockedCells = null;
		_nonUnits = null;
	}

	public bool TryApplyAndEnqueue(IAction action) =>
		action is IBattleAction battleAction && _sim.TryEnqueue(battleAction);

	public void ForceApplyAndEnqueue(IAction action)
	{
		if (action is IBattleAction battleAction)
			_sim.ForceEnqueue(battleAction);
	}

	public void EnqueueMovePath(string actorId, Option option)
	{
		EnsureBoard();
		var origin = Board.StateOf(actorId).Position;
		var frame = BodyFrame.From(Board.StateOf(actorId));
		var steps = MoveStepAction.BuildSteps(actorId, frame, origin, option.Path);
		var group = _sim.AllocateUndoGroup();

		foreach (var step in steps)
			_sim.ForceEnqueue(new MoveStepAction(
				step.OwnerId,
				step.From,
				step.To,
				step.UsedDirectionsMaskBefore,
				group));

		_sim.Refresh();
	}

	public bool TryEnqueueMovePath(string actorId, Option option)
	{
		if (_sim.State.IsMovePathStarted)
			return false;

		EnqueueMovePath(actorId, option);
		return true;
	}

	public bool TryUndoLast() => _sim.TryUndoLast();

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
			ActionApplicator.ApplyOne(
				scheduled,
				Board,
				_sim.State,
				[],
				_sim.Timeline,
				_ownerId!));
	}

	private BattleBoard CreateWorldFromSnapshot() =>
		BattleBoard.FromSnapshot(_roster!, _nonUnits!, _grid!, _blockedCells!);

	private IReadOnlyList<IBattleAction> ExpandPlayback(IReadOnlyList<IBattleAction> actions) =>
		ActionApplicator.WithPhaseEnd(actions, _ownerId);

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
