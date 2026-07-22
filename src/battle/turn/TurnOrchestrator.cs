using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Core;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Turn;
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

		var playerPhaseContext = new TurnPhaseContext();
		var enemyPhaseContext = new TurnPhaseContext();

		var turnStart = commit.TurnStart;
		var tick = turnStart;
		while (tick <= timeline.MaxTick)
		{
			timeline.Clock.Set(tick);

			while (timeline.At(tick).TryDequeue(out var action) && action is not null)
			{
				if (!BattleActionRunner.IsKnown(action))
					continue;

				if (SystemAction.Is(action))
					ApplySystemAction(action, timeline);
				else
					ApplyUnitAction(
						action,
						player,
						enemy,
						playerPhaseContext,
						enemyPhaseContext,
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
		var board = BattleBoard.FromLive(
			_units,
			_hazards.MutableNonUnits,
			_grid,
			_hazards.GetBlockedCells(),
			timeline);
		var ctx = BattleActionContext.For(board, new TurnPhaseContext(), EntityIds.System);
		BattleActionRunner.Apply(action, ctx);
	}

	private void ApplyUnitAction(
		IAction action,
		Unit player,
		Unit enemy,
		TurnPhaseContext playerPhaseContext,
		TurnPhaseContext enemyPhaseContext,
		Timeline timeline)
	{
		var ownerId = action.OwnerId;
		var isPlayer = ownerId == player.State.Id;
		var phaseContext = isPlayer ? playerPhaseContext : enemyPhaseContext;

		var board = BattleBoard.FromLive(
			_units,
			_hazards.MutableNonUnits,
			_grid,
			_hazards.GetBlockedCells(),
			timeline);
		var ctx = BattleActionContext.For(board, phaseContext, ownerId);
		BattleActionRunner.Apply(action, ctx);
	}

	private Dictionary<string, State> SnapshotAll() =>
		_units.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());
}
