using GrimSpace.Math.Grid;
using GrimSpace.Battle.Units;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Movement;

public interface IMovement
{
	IReadOnlyList<Option> GetMoveOptions(State unit, BoundedGrid grid, IReadOnlySet<Coord> blockedCells);
	bool CanMove(State unit, Option option);
	void ApplyMove(State unit, Option option);
	void ApplyMomentum(State unit, IReadOnlyList<Coord> path);
}
