using System;
using System.Collections.Generic;

namespace GrimSpace.Battle;

public sealed class BattleState
{
	public const int DefaultMoveRange = 3;

	public BattleGrid Grid { get; }
	public IReadOnlyList<UnitState> Units => _units;

	private readonly List<UnitState> _units;

	public UnitState Player { get; }
	public UnitState Enemy { get; }

	public BattleState(int width, int height, int depth, GridCoord playerStart, GridCoord enemyStart)
	{
		Grid = new BattleGrid(width, height, depth);

		if (!Grid.IsInBounds(playerStart) || !Grid.IsInBounds(enemyStart))
			throw new ArgumentException("Start positions must be inside the grid.");

		if (playerStart == enemyStart)
			throw new ArgumentException("Start positions must not overlap.");

		Player = new UnitState(1, Team.Player, playerStart, isMobile: true);
		Enemy = new UnitState(2, Team.Enemy, enemyStart, isMobile: false);

		_units = [Player, Enemy];
	}

	public UnitState? GetUnitAt(GridCoord coord)
	{
		foreach (var unit in _units)
		{
			if (unit.Position == coord)
				return unit;
		}

		return null;
	}

	public bool CanMoveTo(UnitState unit, GridCoord destination, int moveRange = DefaultMoveRange)
	{
		if (!unit.IsMobile)
			return false;

		if (!Grid.IsInBounds(destination))
			return false;

		if (GetUnitAt(destination) is not null)
			return false;

		return unit.Position.ManhattanDistanceTo(destination) <= moveRange;
	}

	public bool TryMove(UnitState unit, GridCoord destination, int moveRange = DefaultMoveRange)
	{
		if (!CanMoveTo(unit, destination, moveRange))
			return false;

		unit.SetPosition(destination);
		return true;
	}
}
