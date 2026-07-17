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
		var enemyCount = commit.EnemyPlan.Actions.Count;
		IReadOnlyDictionary<string, State>? unitsAfterPlayer = null;

		var playerTags = new BattleTurnTags();
		var enemyTags = new BattleTurnTags();
		var playerActionsApplied = new List<IBattleAction>();
		var enemyActionsApplied = new List<IBattleAction>();

		var index = 0;
		while (commit.Queue.TryDequeue(out var action) && action is not null)
		{
			if (action is IBattleAction battleAction)
				ApplyBattleAction(
					battleAction,
					commit,
					player,
					playerTags,
					enemyTags,
					playerActionsApplied,
					enemyActionsApplied);

			applied.Add(action);
			sink?.OnActionApplied(new PresentationEvent(action));
			index++;

			if (index == playerCount)
			{
				FinalizeActorPhase(player, commit.PlayerPlan);
				unitsAfterPlayer = SnapshotAll();
			}
		}

		if (playerCount == 0)
		{
			FinalizeActorPhase(player, commit.PlayerPlan);
			unitsAfterPlayer = SnapshotAll();
		}

		var enemy = _units.First(unit => unit.Controller == EController.Enemy);
		if (enemy.State.IsAlive)
			FinalizeActorPhase(enemy, commit.EnemyPlan);

		return new TurnExecutionResult(applied, unitsAfterPlayer ?? SnapshotAll());
	}

	private void ApplyBattleAction(
		IBattleAction action,
		TurnCommitResult commit,
		Unit player,
		BattleTurnTags playerTags,
		BattleTurnTags enemyTags,
		List<IBattleAction> playerActionsApplied,
		List<IBattleAction> enemyActionsApplied)
	{
		var ownerId = ((IAction)action).OwnerId;
		var isPlayer = ownerId == player.State.Id;
		var tags = isPlayer ? playerTags : enemyTags;
		var startFacing = isPlayer ? commit.PlayerPlan.StartFacing : commit.EnemyPlan.StartFacing;
		var applied = isPlayer ? playerActionsApplied : enemyActionsApplied;
		var context = new BattlePlanContext(applied, startFacing, tags);

		BattlePlanExecutor.ApplyCommittedAction(
			action,
			_units,
			_grid,
			_hazards.MutableNonUnits,
			_hazards.GetBlockedCells(),
			context,
			ownerId);

		applied.Add(action);
	}

	private static void FinalizeActorPhase(Unit actor, UnitPlan plan)
	{
		if (!plan.BattleActions.Any(action => action is MoveAction))
			actor.State.MomentumLevel = System.Math.Max(actor.State.MomentumLevel - 1, 0);
	}

	public void ExecuteEnvironmentPhase() =>
		_hazards.ResolveAgainst(_units);

	private Dictionary<string, State> SnapshotAll() =>
		_units.ToDictionary(unit => unit.State.Id, unit => unit.State.Clone());
}
