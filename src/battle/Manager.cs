using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Math.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.Movement.Enums;
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
	public IReadOnlyList<IAction> PlannedActions => _planQueue.Actions;
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }

	private readonly PlanQueue _planQueue = new();
	private readonly List<Hazard> _activeHazards = [];

	public int MissilesRemainingThisTurn =>
		System.Math.Max(CombatConfig.MissilesPerTurn - _planQueue.CountOf<MissileAction>(), 0);

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

		return new Manager(grid, turn, units);
	}

	public Unit? GetPlayer() =>
		Units.FirstOrDefault(u => u.Controller == EController.Player);

	public Unit? GetEnemy() =>
		Units.FirstOrDefault(u => u.Controller == EController.Enemy);

	public SimulatedTurn GetSimulation()
	{
		var player = GetPlayer()!;
		var enemy = GetEnemy()!;
		return PlanExecutor.Simulate(player, enemy, Grid, _planQueue.Actions);
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

		var simulation = GetSimulation();
		return new Preview
		{
			Options = unit.Movement.GetPreviews(simulation.Player, Grid),
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
		var enemy = GetEnemy();
		if (enemy is null || activeUnit is null || !CanPlanRailgun(activeUnit, enemy))
			return cells;

		cells.Add(GetSimulation().Enemy.Position);
		return cells;
	}

	public bool EnqueueMove(Unit unit, Option option)
	{
		if (!CanAct(unit))
			return false;

		var simulation = GetSimulation();
		if (!StepCosts.CanAffordMove(simulation.Player, option))
			return false;

		if (!unit.Movement.CanMove(simulation.Player, option))
			return false;

		_planQueue.ReplaceOrAdd(new MoveAction(option), action => action is MoveAction);
		return true;
	}

	public bool EnqueueRoll(Unit unit, ERollDirection direction)
	{
		if (!CanAct(unit))
			return false;

		var simulation = GetSimulation();
		if (simulation.Player.ActionPoints < CombatConfig.RollApCost)
			return false;

		_planQueue.Add(new RollAction(direction));
		return true;
	}

	public bool EnqueueHeadingTurn(Unit unit, EHeadingTurn turn)
	{
		if (!CanAct(unit))
			return false;

		var simulation = GetSimulation();
		var cost = CombatConfig.HeadingTurnBaseApCost + simulation.Player.MomentumLevel;
		if (simulation.Player.ActionPoints < cost)
			return false;

		_planQueue.Add(new HeadingTurnAction(turn));
		return true;
	}

	public bool CanPlanMissileAt(Unit unit, Coord center, EMissileMount mount)
	{
		if (!CanAct(unit) || unit.Controller != EController.Player)
			return false;

		if (MissilesRemainingThisTurn <= 0)
			return false;

		var simulation = GetSimulation();
		var config = MissileMountConfig.For(mount);
		return MissileTargeting.IsValidTarget(
			simulation.Player.Position,
			simulation.Player.ForwardDirection,
			simulation.Player.RightDirection,
			simulation.Player.UpDirection,
			center,
			config,
			Grid.IsInBounds);
	}

	public HashSet<Coord> GetValidMissileCells(Unit unit, EMissileMount mount)
	{
		var simulation = GetSimulation();
		var config = MissileMountConfig.For(mount);
		return MissileTargeting.GetValidCells(
			simulation.Player.Position,
			simulation.Player.ForwardDirection,
			simulation.Player.RightDirection,
			simulation.Player.UpDirection,
			config,
			Grid.IsInBounds);
	}

	public bool EnqueueMissile(Unit unit, Coord center, EMissileMount mount)
	{
		if (!CanPlanMissileAt(unit, center, mount))
			return false;

		_planQueue.Add(new MissileAction(center, mount));
		return true;
	}

	public bool CanPlanRailgun(Unit attacker, Unit target)
	{
		if (!CanAct(attacker) || attacker.Controller != EController.Player)
			return false;

		if (target.Controller == attacker.Controller)
			return false;

		var simulation = GetSimulation();
		if (!simulation.Enemy.IsAlive)
			return false;

		if (simulation.Enemy.MomentumLevel != CombatConfig.RailgunRequiredTargetMomentum)
			return false;

		if (simulation.Player.Position.ManhattanDistanceTo(simulation.Enemy.Position)
			> CombatConfig.RailgunMaxRange)
		{
			return false;
		}

		return true;
	}

	public bool EnqueueRailgun(Unit attacker, Unit target)
	{
		if (!CanPlanRailgun(attacker, target))
			return false;

		_planQueue.ReplaceOrAdd(
			new RailgunAction(target.State.Id),
			action => action is RailgunAction);
		return true;
	}

	public bool TryUndoLast() => _planQueue.TryPopLast(out _);

	public bool RequestEndTurn()
	{
		if (IsBattleOver)
			return false;

		ExecutePlan();

		if (!IsBattleOver)
			RunEnemyTurn();

		ResolveHazards();
		_activeHazards.Clear();

		foreach (var unit in Units)
			unit.State.ActionPoints = unit.State.Stats.MaxAp;

		_planQueue.Clear();
		Turn.AdvanceTurn();

		var player = GetPlayer();
		if (player is not null)
			Turn.SetActiveUnit(player.State.Id);

		return true;
	}

	private void ExecutePlan()
	{
		var player = GetPlayer();
		var enemy = GetEnemy();
		if (player is null || enemy is null)
			return;

		PlanExecutor.Apply(
			_planQueue.Actions,
			player,
			enemy,
			Grid,
			_activeHazards);

		CheckBattleOver();
	}

	private void RunEnemyTurn()
	{
		var enemy = GetEnemy();
		if (enemy is null || !enemy.State.IsAlive)
			return;

		var hazardCells = GetExecutedHazardCells();
		var move = EnemyPlanner.ChooseMove(enemy, Grid, hazardCells);
		if (move is null)
			return;

		enemy.Movement.ApplyMove(enemy.State, move);
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
