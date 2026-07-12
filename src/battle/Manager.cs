using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Combat;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Domain.Combat;
using GrimSpace.Domain.Grid;
using GrimSpace.Domain.Units.Enums;
using BattleGrid = GrimSpace.Battle.Grid.Grid;
using EnemyAi = GrimSpace.Battle.Ai.Enemy;
using UnitState = GrimSpace.Battle.Units.State;

namespace GrimSpace.Battle;

public sealed class Manager
{
	public BattleGrid Grid { get; }
	public Turn.Manager Turn { get; }
	public IReadOnlyList<Unit> Units { get; }
	public IReadOnlyList<PlannedAction> PlannedActions => _planQueue.Actions;
	public bool IsBattleOver { get; private set; }
	public string? WinnerId { get; private set; }

	private readonly PlanQueue _planQueue = new();
	private readonly List<Hazard> _activeHazards = [];

	public int MissilesRemainingThisTurn =>
		Math.Max(CombatConfig.MissilesPerTurn - _planQueue.CountOf<PlannedMissile>(), 0);

	private Manager(BattleGrid grid, Turn.Manager turn, IReadOnlyList<Unit> units)
	{
		Grid = grid;
		Turn = turn;
		Units = units;
	}

	public static Manager FromEncounter(Domain.Run.Encounter encounter, int gridSize = CombatConfig.DefaultGridSize)
	{
		var grid = new BattleGrid(gridSize, gridSize, gridSize);
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
		return PlanSimulator.Simulate(player, enemy, Grid, _planQueue.Actions);
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
			Options = unit.Movement.GetPreviews(simulation.Player, Grid, unit.Actions),
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
		var moveAction = new MoveAction(option);
		if (!unit.Actions.CanPerform(moveAction, simulation.Player))
			return false;

		if (!unit.Movement.CanMove(simulation.Player, option))
			return false;

		_planQueue.ReplaceOrAdd(new PlannedMove(option), action => action is PlannedMove);
		return true;
	}

	public bool EnqueueRoll(Unit unit, ERollDirection direction)
	{
		if (!CanAct(unit))
			return false;

		var simulation = GetSimulation();
		if (simulation.Player.ActionPoints < CombatConfig.RollApCost)
			return false;

		_planQueue.Add(new PlannedRoll(direction));
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

		_planQueue.Add(new PlannedHeadingTurn(turn));
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

		_planQueue.Add(new PlannedMissile(center, mount));
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
			new PlannedRailgun(target.State.Id),
			action => action is PlannedRailgun);
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
		if (player is null)
			return;

		foreach (var action in _planQueue.Actions)
		{
			switch (action)
			{
				case PlannedMove move:
					player.Movement.ApplyMove(player.State, move.Option);
					player.State.ActionPoints -= move.Option.ApCost;
					break;

				case PlannedRoll roll:
					Orientation.ApplyRoll(player.State, roll.Direction);
					player.State.ActionPoints -= CombatConfig.RollApCost;
					break;

				case PlannedHeadingTurn headingTurn:
					var turnCost = CombatConfig.HeadingTurnBaseApCost + player.State.MomentumLevel;
					Orientation.ApplyHeadingTurn(player.State, headingTurn.Turn);
					player.State.ActionPoints -= turnCost;
					break;

				case PlannedMissile missile:
					_activeHazards.Add(Hazard.MissileZone(
						missile.Center,
						Grid,
						CombatConfig.MissileRadius,
						CombatConfig.MissileDamage,
						CombatConfig.MissileMomentumLoss));
					break;

				case PlannedRailgun railgun:
					var target = Units.FirstOrDefault(u => u.State.Id == railgun.TargetUnitId);
					if (target is not null)
					{
						ApplyDamage(target.State, CombatConfig.RailgunDamage);
						CheckBattleOver();
					}

					break;
			}

			if (IsBattleOver)
				return;
		}
	}

	private void RunEnemyTurn()
	{
		var enemy = GetEnemy();
		if (enemy is null || !enemy.State.IsAlive)
			return;

		var hazardCells = GetExecutedHazardCells();
		var move = EnemyAi.ChooseMove(enemy, Grid, enemy.Actions, hazardCells);
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
			enemy.State.MomentumLevel = Math.Max(
				enemy.State.MomentumLevel - hazard.MomentumLoss,
				0);
		}

		CheckBattleOver();
	}

	private static void ApplyDamage(UnitState target, int damage) =>
		target.Hp = Math.Max(target.Hp - damage, 0);

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
