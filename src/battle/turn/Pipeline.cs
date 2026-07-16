using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Debug;
using GrimSpace.Battle.Environment;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Math.Grid;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle.Turn;

public sealed class Pipeline
{
	private readonly BoundedGrid _grid;
	private readonly IReadOnlyList<Unit> _units;
	private readonly Manager _turn;
	private readonly HazardSystem _hazards;

	public Pipeline(
		BoundedGrid grid,
		IReadOnlyList<Unit> units,
		Manager turn,
		HazardSystem hazards)
	{
		_grid = grid;
		_units = units;
		_turn = turn;
		_hazards = hazards;
	}

	public PipelineResult Resolve(FinalizedPlan playerPlan, Func<Unit?> getPlayer, Func<Unit?> getEnemy)
	{
		var turnNumber = _turn.TurnNumber;
		var unitsAtTurnStart = SnapshotAll();

		var player = getPlayer();
		var enemy = getEnemy();

		ExecutePlayerPhase(playerPlan, player, enemy);
		var outcome = RulesEngine.Evaluate(_units);
		var unitsAfterPlayer = SnapshotAll();

		IReadOnlyList<IBattleAction> enemyActions = [];
		if (!outcome.IsOver)
			enemyActions = ExecuteEnemyPhase(enemy, player);

		outcome = RulesEngine.Evaluate(_units);
		var unitsAfterEnemy = SnapshotAll();

		var hazardsBeforeResolve = _hazards.Active.ToList();
		if (!outcome.IsOver)
			ExecuteEnvironmentPhase();

		outcome = RulesEngine.Evaluate(_units);

		ExecuteUpkeepPhase(getPlayer);

		StateLog.LogTurnResolution(
			turnNumber,
			playerPlan.Actions,
			enemyActions,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			unitsAfterPlayer,
			unitsAfterEnemy,
			SnapshotAll());

		return new PipelineResult(outcome.IsOver, outcome.WinnerId);
	}

	private void ExecutePlayerPhase(
		FinalizedPlan playerPlan,
		Unit? player,
		Unit? enemy)
	{
		if (player is null || enemy is null)
			return;

		BattlePlanExecutor.Apply(
			playerPlan.Actions,
			player,
			enemy,
			_grid,
			_hazards.RegisterTarget,
			_hazards.GetBlockedCells(),
			playerPlan.StartFacing);
	}

	private IReadOnlyList<IBattleAction> ExecuteEnemyPhase(Unit? enemy, Unit? player)
	{
		if (enemy is null || player is null || !enemy.State.IsAlive)
			return [];

		var actions = EnemyPlanner.PlanTurn(
			enemy,
			player,
			_grid,
			_hazards.GetOccupiedCells(),
			_hazards.GetBlockedCells());
		if (actions.Count == 0)
			return actions;

		var startFacing = GridBasis.From(
			enemy.State.ForwardDirection,
			enemy.State.UpDirection,
			enemy.State.RightDirection);

		BattlePlanExecutor.Apply(
			actions,
			enemy,
			player,
			_grid,
			_hazards.RegisterTarget,
			_hazards.GetBlockedCells(),
			startFacing);

		return actions;
	}

	private void ExecuteEnvironmentPhase()
	{
		_hazards.ResolveAgainst(_units);
		_hazards.Clear();
	}

	private void ExecuteUpkeepPhase(Func<Unit?> getPlayer)
	{
		foreach (var unit in _units)
			unit.State.ActionPoints = unit.State.Stats.MaxAp;

		_turn.AdvanceTurn();

		var player = getPlayer();
		if (player is not null)
			_turn.SetActiveUnit(player.State.Id);
	}

	private Dictionary<string, UnitState> SnapshotAll() =>
		_units.ToDictionary(u => u.State.Id, u => u.State.Clone());
}

public readonly record struct PipelineResult(bool IsBattleOver, string? WinnerId);
