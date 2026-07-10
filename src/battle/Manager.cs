using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Units.Enums;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle;

public sealed class Manager
{
	public BattleGrid Grid { get; }
	public Turn.Manager Turn { get; }
	public IReadOnlyList<Unit> Units { get; }

	private Manager(BattleGrid grid, Turn.Manager turn, IReadOnlyList<Unit> units)
	{
		Grid = grid;
		Turn = turn;
		Units = units;
	}

	public static Manager CreateDefault(int gridSize = 8)
	{
		var grid = new BattleGrid(gridSize, gridSize, gridSize);
		var turn = new Turn.Manager();

		var units = new Unit[]
		{
			Factory.Create(new Blueprint
			{
				Id = "player",
				Type = EType.Fighter,
				Controller = EController.Player,
				Position = new Coord(1, 1, 1),
			}),
			Factory.Create(new Blueprint
			{
				Id = "enemy",
				Type = EType.Fighter,
				Controller = EController.Enemy,
				Position = new Coord(6, 6, 6),
			}),
		};

		turn.SetActiveUnit(units[0].State.Id);
		return new Manager(grid, turn, units);
	}

	public IEnumerable<Unit> GetActiveUnits() =>
		Units.Where(u => Turn.IsActive(u.State.Id));

	public IEnumerable<(Unit Unit, Preview Preview)> ShowMovementForActiveUnits()
	{
		foreach (var unit in GetActiveUnits())
		{
			var preview = unit.ShowMovement(Grid);
			if (preview is not null)
				yield return (unit, preview);
		}
	}

	public int GetMoveApCost(Unit unit, Option option) =>
		unit.Actions.GetApCost(new MoveAction(option), unit.State);

	public bool RequestMove(Unit unit, Option option)
	{
		if (!Turn.IsActive(unit.State.Id))
			return false;

		var moveAction = new MoveAction(option);
		if (!unit.Actions.CanPerform(moveAction, unit.State))
			return false;

		if (!unit.Movement.CanMove(unit.State, option))
			return false;

		var cost = unit.Actions.GetApCost(moveAction, unit.State);
		if (unit.State.ActionPoints < cost)
			return false;

		unit.Movement.ApplyMove(unit.State, option);
		unit.Actions.ApplyCost(moveAction, unit.State);
		return true;
	}
}
