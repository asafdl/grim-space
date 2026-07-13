using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Debug;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle;

public sealed class Manager
{
	public BoundedGrid Grid { get; }
	public Turn.Manager Turn { get; }
	public IReadOnlyList<Unit> Units { get; }
	public IReadOnlyList<IBattleAction> PlannedActions => _playerPlan.Actions;
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }

	private readonly PlayerPlan _playerPlan = new();
	private readonly List<Hazard> _activeHazards = [];

	public void BeginPlanning(UnitState player) => _playerPlan.ResetFrom(player);

	public int MissilesRemainingThisTurn => GetPlanContext().MissilesRemaining;

	private Manager(BoundedGrid grid, Turn.Manager turn, IReadOnlyList<Unit> units)
	{
		Grid = grid;
		Turn = turn;
		Units = units;
	}

	public static Manager FromEncounter(Encounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BoundedGrid(gridSize, gridSize, gridSize);
		var turn = new Turn.Manager();

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(spawn.Unit, spawn.Position, spawn.InitialMomentum))
			.ToArray();

		var firstPlayer = units.FirstOrDefault(u => u.Controller == EController.Player);
		if (firstPlayer is not null)
			turn.SetActiveUnit(firstPlayer.State.Id);

		var manager = new Manager(grid, turn, units);
		var player = manager.GetPlayer();
		if (player is not null)
			manager.BeginPlanning(player.State);
		return manager;
	}

	public Unit? GetPlayer() =>
		Units.FirstOrDefault(u => u.Controller == EController.Player);

	public Unit? GetEnemy() =>
		Units.FirstOrDefault(u => u.Controller == EController.Enemy);

	public SimulatedTurn GetSimulation()
	{
		var player = GetPlayer()!;
		var enemy = GetEnemy()!;
		return BattlePlanExecutor.Simulate(player, enemy, Grid, _playerPlan);
	}

	public HashSet<Coord> GetPlannedHazardCells()
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in GetSimulation().Hazards)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	private HashSet<Coord> GetExecutedHazardCells()
	{
		var cells = new HashSet<Coord>();
		foreach (var hazard in _activeHazards)
			cells.UnionWith(hazard.Cells);

		return cells;
	}

	public HashSet<Coord> GetMissilePreviewCells(Coord center)
	{
		var hazard = Hazard.MissileZone(
			center,
			Grid,
			CombatConfig.MissileRadius,
			CombatConfig.MissileDamage,
			CombatConfig.MissileMomentumLoss);
		return hazard.Cells;
	}

	public Preview? GetMovementPreview(Unit unit)
	{
		if (!CanAct(unit))
			return null;

		var board = GetPlanBoard();
		var context = GetPlanContext();
		return new Preview
		{
			Options = LegalActions.GetMoveOptions(board, context),
		};
	}

	public Unit? GetActivePlayer() =>
		GetActiveUnits().FirstOrDefault(u => u.Controller == EController.Player);

	public (Unit? Unit, Preview? Preview) GetActivePlayerPreview()
	{
		var unit = GetActivePlayer();
		if (unit is null)
			return (null, null);

		return (unit, GetMovementPreview(unit));
	}

	public HashSet<Coord> GetRailgunTargetCells(Unit? activeUnit)
	{
		var cells = new HashSet<Coord>();
		if (activeUnit is null || !CanAct(activeUnit))
			return cells;

		var board = GetPlanBoard();
		var context = GetPlanContext();
		if (!LegalActions.IsRailgunAvailable(board, context))
			return cells;

		cells.Add(board.Enemy.Position);
		return cells;
	}

	public bool IsLegal(IBattleAction action)
	{
		var player = GetActivePlayer();
		return player is not null && CanAct(player) && action.IsLegal(GetPlanBoard(), GetPlanContext());
	}

	public bool TryEnqueue(IBattleAction action)
	{
		var player = GetActivePlayer();
		if (player is null || !CanAct(player))
			return false;

		if (action is HeadingTurnAction heading && Orientation.IsYawTurn(heading.Turn))
		{
			_playerPlan.Enqueue(action);
			if (!CanAffordPlan())
			{
				_playerPlan.TryUndoLast();
				return false;
			}

			return true;
		}

		if (!IsLegal(action))
			return false;

		_playerPlan.Enqueue(action);
		return true;
	}

	public HashSet<Coord> GetValidMissileCells(EMissileMount mount)
	{
		var player = GetActivePlayer();
		if (player is null || !CanAct(player))
			return [];

		return LegalActions.GetMissileCells(GetPlanBoard(), GetPlanContext(), mount);
	}

	private BattlePlanContext GetPlanContext() => _playerPlan.Context;

	private BattleBoard GetPlanBoard()
	{
		var player = GetPlayer()!;
		var enemy = GetEnemy()!;
		return BattlePlanExecutor.BuildPlanBoard(player, enemy, Grid, _playerPlan);
	}

	private bool CanAffordPlan() =>
		GetPlanBoard().Player.ActionPoints >= 0;

	public bool TryUndoLast() => _playerPlan.TryUndoLast();

	public bool RequestEndTurn()
	{
		if (IsBattleOver)
			return false;

		var turnNumber = Turn.TurnNumber;
		var plannedActions = _playerPlan.Actions.ToList();
		var unitsAtTurnStart = SnapshotAll();

		ExecutePlan();
		var unitsAfterPlayer = SnapshotAll();

		Option? enemyMove = null;
		if (!IsBattleOver)
			enemyMove = RunEnemyTurn();

		var unitsAfterEnemy = SnapshotAll();

		var hazardsBeforeResolve = _activeHazards.ToList();
		ResolveHazards();
		_activeHazards.Clear();

		foreach (var unit in Units)
			unit.State.ActionPoints = unit.State.Stats.MaxAp;

		StateLog.LogTurnResolution(
			turnNumber,
			plannedActions,
			enemyMove,
			hazardsBeforeResolve,
			unitsAtTurnStart,
			unitsAfterPlayer,
			unitsAfterEnemy,
			SnapshotAll());

		Turn.AdvanceTurn();

		var player = GetPlayer();
		if (player is not null)
		{
			Turn.SetActiveUnit(player.State.Id);
			BeginPlanning(player.State);
		}

		return true;
	}

	private void ExecutePlan()
	{
		var player = GetPlayer();
		var enemy = GetEnemy();
		if (player is null || enemy is null)
			return;

		BattlePlanExecutor.Apply(
			_playerPlan.Actions,
			player,
			enemy,
			Grid,
			_activeHazards,
			_playerPlan);

		CheckBattleOver();
	}

	private Option? RunEnemyTurn()
	{
		var enemy = GetEnemy();
		if (enemy is null || !enemy.State.IsAlive)
			return null;

		var hazardCells = GetExecutedHazardCells();
		var move = EnemyPlanner.ChooseMove(enemy, Grid, hazardCells);
		if (move is null)
			return null;

		enemy.Movement.ApplyMove(enemy.State, move);
		return move;
	}

	private void ResolveHazards()
	{
		var enemy = GetEnemy();
		if (enemy is null || !enemy.State.IsAlive)
			return;

		foreach (var hazard in _activeHazards)
		{
			if (!hazard.Cells.Contains(enemy.State.Position))
				continue;

			ApplyDamage(enemy.State, hazard.Damage);
			enemy.State.MomentumLevel = System.Math.Max(
				enemy.State.MomentumLevel - hazard.MomentumLoss,
				0);
		}

		CheckBattleOver();
	}

	private static void ApplyDamage(UnitState target, int damage) =>
		target.Hp = System.Math.Max(target.Hp - damage, 0);

	private Dictionary<string, UnitState> SnapshotAll() =>
		Units.ToDictionary(u => u.State.Id, u => u.State.Clone());

	private void CheckBattleOver()
	{
		var enemy = GetEnemy();
		if (enemy is not null && !enemy.State.IsAlive)
		{
			IsBattleOver = true;
			WinnerId = GetPlayer()?.State.Id;
			return;
		}

		var player = GetPlayer();
		if (player is not null && !player.State.IsAlive)
		{
			IsBattleOver = true;
			WinnerId = enemy?.State.Id;
		}
	}

	private bool CanAct(Unit unit) =>
		!IsBattleOver && Turn.IsActive(unit.State.Id) && unit.State.IsAlive;

	public IEnumerable<Unit> GetActiveUnits() =>
		Units.Where(u => Turn.IsActive(u.State.Id) && u.State.IsAlive);
}
