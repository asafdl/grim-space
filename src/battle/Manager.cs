using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Units;
using GrimSpace.Domain.Run;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle;

public sealed class Manager
{
	public BattleGrid Grid { get; }
	public Turn.Manager Turn { get; }
	public IReadOnlyList<Unit> Units { get; }

	private readonly Dictionary<string, Option> _pendingMoves = new();

	private Manager(BattleGrid grid, Turn.Manager turn, IReadOnlyList<Unit> units)
	{
		Grid = grid;
		Turn = turn;
		Units = units;
	}

	public static Manager FromEncounter(Encounter encounter, int gridSize = 8)
	{
		var grid = new BattleGrid(gridSize, gridSize, gridSize);
		var turn = new Turn.Manager();

		var units = encounter.Spawns
			.Select(spawn => Factory.Create(spawn.Unit, spawn.Position))
			.ToArray();

		var firstPlayer = units.FirstOrDefault(u => u.Controller == Domain.Units.Enums.EController.Player);
		if (firstPlayer is not null)
			turn.SetActiveUnit(firstPlayer.State.Id);

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

	public bool CommitMove(Unit unit, Option option)
	{
		if (!Turn.IsActive(unit.State.Id))
			return false;

		var moveAction = new MoveAction(option);
		if (!unit.Actions.CanPerform(moveAction, unit.State))
			return false;

		if (!unit.Movement.CanMove(unit.State, option))
			return false;

		if (_pendingMoves.TryGetValue(unit.State.Id, out var existing))
			unit.State.ActionPoints += existing.ApCost;

		_pendingMoves[unit.State.Id] = option;
		unit.Actions.ApplyCost(moveAction, unit.State);
		return true;
	}

	public bool RequestEndTurn()
	{
		foreach (var unit in Units)
		{
			if (_pendingMoves.TryGetValue(unit.State.Id, out var move))
				unit.Movement.ApplyMove(unit.State, move);

			unit.State.ActionPoints = unit.State.Stats.MaxAp;
		}

		_pendingMoves.Clear();
		Turn.AdvanceTurn();
		return true;
	}
}
