using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Presentation.Events;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
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
		IPresentationEventSink? sink = null)
	{
		var applied = new List<IAction>();
		var player = _units.First(unit => unit.Controller == EController.Player);
		var playerCount = commit.PlayerPlan.Actions.Count;
		IReadOnlyDictionary<string, State>? unitsAfterPlayer = null;

		var playerTurnState = new TurnState();
		var enemyTurnState = new TurnState();
		var playerActionsApplied = new List<IAction>();
		var enemyActionsApplied = new List<IAction>();

		var index = 0;
		while (commit.Queue.TryDequeue(out var action) && action is not null)
		{
			ApplyBattleAction(
				action,
				commit,
				player,
				playerTurnState,
				enemyTurnState,
				playerActionsApplied,
				enemyActionsApplied);

			applied.Add(action);
			sink?.OnActionApplied(new PresentationEvent(action));
			index++;

			if (index == playerCount)
			{
				TurnPlanner.RunPhaseEnd(player.State, commit.PlayerPlan.Actions);
				unitsAfterPlayer = SnapshotAll();
			}
		}

		if (playerCount == 0)
		{
			TurnPlanner.RunPhaseEnd(player.State, commit.PlayerPlan.Actions);
			unitsAfterPlayer = SnapshotAll();
		}

		var enemy = _units.First(unit => unit.Controller == EController.Enemy);
		if (enemy.State.IsAlive)
			TurnPlanner.RunPhaseEnd(enemy.State, commit.EnemyPlan.Actions);

		return new TurnExecutionResult(applied, unitsAfterPlayer ?? SnapshotAll());
	}

	private void ApplyBattleAction(
		IAction action,
		TurnCommitResult commit,
		Unit player,
		TurnState playerTurnState,
		TurnState enemyTurnState,
		List<IAction> playerActionsApplied,
		List<IAction> enemyActionsApplied)
	{
		var ownerId = action.OwnerId;
		var isPlayer = ownerId == player.State.Id;
		var turnState = isPlayer ? playerTurnState : enemyTurnState;
		var applied = isPlayer ? playerActionsApplied : enemyActionsApplied;
		var context = new BattlePlanContext(applied, turnState);

		TurnPlanner.ApplyCommittedAction(
			action,
			_units,
			_grid,
			_hazards.MutableNonUnits,
			_hazards.GetBlockedCells(),
			context,
			ownerId);

		applied.Add(action);
	}

	public void ExecuteEnvironmentPhase() =>
		_hazards.ResolveAgainst(_units);

	private Dictionary<string, State> SnapshotAll() =>
		_units.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());
}
