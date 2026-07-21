using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Turn;

public sealed record TurnExecutionResult(
	IReadOnlyList<IAction> Applied,
	IReadOnlyDictionary<string, State> UnitsAfterPlayer);

public sealed class TurnOrchestrator
{
	private readonly BoundedGrid _grid;
	private readonly IReadOnlyList<Unit> _units;
	private readonly HazardSystem _hazards;

	public TurnOrchestrator(
		BoundedGrid grid,
		IReadOnlyList<Unit> units,
		HazardSystem hazards)
	{
		_grid = grid;
		_units = units;
		_hazards = hazards;
	}

	public TurnExecutionResult Execute(
		TurnCommitResult commit,
		Timeline timeline,
		IPresentationEventSink? sink = null)
	{
		var applied = new List<IAction>();
		var player = _units.First(unit => unit.Controller == EController.Player);
		var enemy = _units.First(unit => unit.Controller == EController.Enemy);
		IReadOnlyDictionary<string, State>? unitsAfterPlayer = null;

		var playerTurnState = new TurnState();
		var enemyTurnState = new TurnState();
		var playerActionsApplied = new List<IAction>();
		var enemyActionsApplied = new List<IAction>();

		var turnStart = commit.TurnStart;
		var tick = turnStart;
		while (tick <= timeline.MaxTick)
		{
			timeline.Clock.Set(tick);

			while (timeline.At(tick).TryDequeue(out var action) && action is not null)
			{
				if (SystemAction.Is(action))
					ApplySystemAction(action, timeline);
				else
					ApplyUnitAction(
						action,
						player,
						enemy,
						playerTurnState,
						enemyTurnState,
						playerActionsApplied,
						enemyActionsApplied,
						timeline);

				applied.Add(action);
				sink?.OnActionApplied(new PresentationEvent(action));
			}

			if (tick == turnStart + TurnPhases.Player)
				unitsAfterPlayer = SnapshotAll();

			tick++;
		}

		return new TurnExecutionResult(applied, unitsAfterPlayer ?? SnapshotAll());
	}

	private void ApplySystemAction(IAction action, Timeline timeline)
	{
		var context = new BattlePlanContext([], new TurnState());
		ActionApplicator.ApplyCommittedAction(
			action,
			_units,
			_grid,
			_hazards.MutableNonUnits,
			_hazards.GetBlockedCells(),
			context,
			timeline,
			EntityIds.System);
	}

	private void ApplyUnitAction(
		IAction action,
		Unit player,
		Unit enemy,
		TurnState playerTurnState,
		TurnState enemyTurnState,
		List<IAction> playerActionsApplied,
		List<IAction> enemyActionsApplied,
		Timeline timeline)
	{
		var ownerId = action.OwnerId;
		var isPlayer = ownerId == player.State.Id;
		var turnState = isPlayer ? playerTurnState : enemyTurnState;
		var applied = isPlayer ? playerActionsApplied : enemyActionsApplied;
		var context = new BattlePlanContext(applied, turnState);

		ActionApplicator.ApplyCommittedAction(
			action,
			_units,
			_grid,
			_hazards.MutableNonUnits,
			_hazards.GetBlockedCells(),
			context,
			timeline,
			ownerId);

		applied.Add(action);
	}

	private Dictionary<string, State> SnapshotAll() =>
		_units.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());
}
